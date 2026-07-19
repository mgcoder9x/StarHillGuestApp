using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Safe outbox replay implementation. Only dead-lettered messages can be selected; dry-run is the default; an
/// explicit maximum prevents unbounded operations; and each successful reset is written to the replay audit table in
/// the same database transaction. Clearing the transport failure state does not erase LastError/LastAttemptAt history.
/// </summary>
public sealed class EfOutboxReplayService<TContext>(TContext context, IClock clock)
    : IOutboxReplayService, IOutboxReplayAuditReader
    where TContext : PlatformDbContext
{
    public async Task<OutboxReplayResult> ExecuteAsync(OutboxReplayRequest request, CancellationToken ct = default)
    {
        Validate(request);
        if (!request.DryRun)
        {
            await EnsureOperationIdIsUnusedAsync(request.OperationId, ct).ConfigureAwait(false);
        }

        var candidates = await SelectCandidatesAsync(request, ct).ConfigureAwait(false);
        if (request.DryRun || candidates.Count == 0)
        {
            return new OutboxReplayResult(request.OperationId, request.DryRun, candidates, 0);
        }

        var strategy = context.Database.CreateExecutionStrategy();
        var replayed = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await EnsureOperationIdIsUnusedAsync(request.OperationId, ct).ConfigureAwait(false);

            var replayedCount = 0;
            var replayedAt = clock.UtcNow;
            var operation = new OutboxReplayOperation
            {
                OperationId = request.OperationId,
                Actor = request.Actor.Trim(),
                Reason = request.Reason.Trim(),
                ReplayedAt = replayedAt,
            };
            context.Set<OutboxReplayOperation>().Add(operation);

            // Claim the operation id before mutating messages. A concurrent reuse fails on the primary key and the
            // transaction rolls back without replaying either selector under an ambiguous audit identity.
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var candidate in candidates)
            {
                var affected = await context.Set<OutboxMessage>()
                    .Where(message => message.Id == candidate.MessageId && message.DeadLetteredAt != null)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(message => message.DeadLetteredAt, (DateTimeOffset?)null)
                            .SetProperty(message => message.NextAttemptAt, replayedAt)
                            .SetProperty(message => message.ErrorCount, 0)
                            .SetProperty(message => message.ClaimId, (Guid?)null)
                            .SetProperty(message => message.ClaimedUntil, (DateTimeOffset?)null),
                        ct)
                    .ConfigureAwait(false);
                if (affected != 1)
                {
                    continue;
                }

                context.Set<OutboxReplayAudit>().Add(new OutboxReplayAudit
                {
                    OperationId = request.OperationId,
                    MessageId = candidate.MessageId,
                    EventType = candidate.EventType,
                    OccurredAt = candidate.OccurredAt,
                    DeadLetteredAt = candidate.DeadLetteredAt,
                    ErrorCount = candidate.ErrorCount,
                    LastError = candidate.LastError,
                    ReplayedAt = replayedAt,
                });
                replayedCount++;
            }

            operation.ReplayedCount = replayedCount;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return replayedCount;
        }).ConfigureAwait(false);

        return new OutboxReplayResult(request.OperationId, false, candidates, replayed);
    }

    public async Task<OutboxReplayAuditRecord?> GetAsync(Guid operationId, CancellationToken ct = default)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Replay OperationId must not be empty.", nameof(operationId));
        }

        var operation = await context.Set<OutboxReplayOperation>()
            .AsNoTracking()
            .Where(item => item.OperationId == operationId)
            .Select(item => new
            {
                item.OperationId,
                item.Actor,
                item.Reason,
                item.ReplayedAt,
                item.ReplayedCount,
            })
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (operation is null)
        {
            return null;
        }

        var auditQuery = context.Set<OutboxReplayAudit>()
            .AsNoTracking()
            .Where(item => item.OperationId == operationId);
        var projection = auditQuery.Select(item => new OutboxReplayAuditMessage(
                item.MessageId,
                item.EventType,
                item.OccurredAt,
                item.DeadLetteredAt,
                item.ErrorCount,
                item.LastError,
                item.ReplayedAt));
        List<OutboxReplayAuditMessage> messages;
        if (context.Database.IsNpgsql())
        {
            messages = await auditQuery
                .OrderBy(item => item.ReplayedAt)
                .ThenBy(item => item.MessageId)
                .Select(item => new OutboxReplayAuditMessage(
                    item.MessageId,
                    item.EventType,
                    item.OccurredAt,
                    item.DeadLetteredAt,
                    item.ErrorCount,
                    item.LastError,
                    item.ReplayedAt))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
        else
        {
            messages = (await projection.ToListAsync(ct).ConfigureAwait(false))
                .OrderBy(item => item.ReplayedAt)
                .ThenBy(item => item.MessageId)
                .ToList();
        }

        return new OutboxReplayAuditRecord(
            operation.OperationId,
            operation.Actor,
            operation.Reason,
            operation.ReplayedAt,
            operation.ReplayedCount,
            messages);
    }

    private async Task EnsureOperationIdIsUnusedAsync(Guid operationId, CancellationToken ct)
    {
        if (await context.Set<OutboxReplayOperation>()
                .AsNoTracking()
                .AnyAsync(operation => operation.OperationId == operationId, ct)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Replay OperationId '{operationId}' has already been used; inspect its audit before retrying.");
        }
    }

    private async Task<List<OutboxReplayCandidate>> SelectCandidatesAsync(
        OutboxReplayRequest request,
        CancellationToken ct)
    {
        if (!context.Database.IsNpgsql())
        {
            return await SelectProviderAgnosticCandidatesAsync(request, ct).ConfigureAwait(false);
        }

        var query = context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(message => message.DeadLetteredAt != null);

        if (request.MessageIds.Count > 0)
        {
            query = query.Where(message => request.MessageIds.Contains(message.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventType = request.EventType.Trim();
            query = query.Where(message => message.EventType == eventType);
        }

        if (request.OccurredFrom is { } from)
        {
            query = query.Where(message => message.OccurredAt >= from);
        }

        if (request.OccurredTo is { } to)
        {
            query = query.Where(message => message.OccurredAt <= to);
        }

        var candidates = await query
            .OrderBy(message => message.DeadLetteredAt)
            .ThenBy(message => message.OccurredAt)
            .Take(request.MaxMessages + 1)
            .Select(message => new OutboxReplayCandidate(
                message.Id,
                message.EventType,
                message.OccurredAt,
                message.DeadLetteredAt!.Value,
                message.ErrorCount,
                message.LastError))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (candidates.Count > request.MaxMessages)
        {
            throw new InvalidOperationException(
                $"Replay matched more than MaxMessages={request.MaxMessages}; narrow the selector and dry-run again.");
        }

        return candidates;
    }

    private async Task<List<OutboxReplayCandidate>> SelectProviderAgnosticCandidatesAsync(
        OutboxReplayRequest request,
        CancellationToken ct)
    {
        // SQLite cannot translate all DateTimeOffset filters/orderings used by the production Npgsql query.
        IEnumerable<OutboxMessage> messages = await context.Set<OutboxMessage>()
            .AsNoTracking()
            .Where(message => message.DeadLetteredAt != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (request.MessageIds.Count > 0)
        {
            messages = messages.Where(message => request.MessageIds.Contains(message.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventType = request.EventType.Trim();
            messages = messages.Where(message => message.EventType == eventType);
        }

        if (request.OccurredFrom is { } from)
        {
            messages = messages.Where(message => message.OccurredAt >= from);
        }

        if (request.OccurredTo is { } to)
        {
            messages = messages.Where(message => message.OccurredAt <= to);
        }

        var candidates = messages
            .OrderBy(message => message.DeadLetteredAt)
            .ThenBy(message => message.OccurredAt)
            .Take(request.MaxMessages + 1)
            .Select(message => new OutboxReplayCandidate(
                message.Id,
                message.EventType,
                message.OccurredAt,
                message.DeadLetteredAt!.Value,
                message.ErrorCount,
                message.LastError))
            .ToList();
        if (candidates.Count > request.MaxMessages)
        {
            throw new InvalidOperationException(
                $"Replay matched more than MaxMessages={request.MaxMessages}; narrow the selector and dry-run again.");
        }

        return candidates;
    }

    private static void Validate(OutboxReplayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperationId == Guid.Empty)
        {
            throw new ArgumentException("Replay OperationId must not be empty.", nameof(request));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);
        if (request.MessageIds is null)
        {
            throw new ArgumentException("Replay MessageIds must not be null.", nameof(request));
        }

        if (request.Actor.Trim().Length > OutboxReplayOperation.MaxActorLength
            || request.Reason.Trim().Length > OutboxReplayOperation.MaxReasonLength)
        {
            throw new ArgumentException("Replay actor or reason exceeds the audit storage limit.", nameof(request));
        }

        if (request.MaxMessages is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Replay MaxMessages must be between 1 and 1000.");
        }

        if (request.MessageIds.Count > request.MaxMessages)
        {
            throw new ArgumentException("Replay MessageIds exceeds MaxMessages.", nameof(request));
        }

        if (request.OccurredFrom > request.OccurredTo)
        {
            throw new ArgumentException("Replay OccurredFrom must not be later than OccurredTo.", nameof(request));
        }

        var hasSelector = request.MessageIds.Count > 0
            || !string.IsNullOrWhiteSpace(request.EventType)
            || request.OccurredFrom is not null
            || request.OccurredTo is not null;
        if (!hasSelector)
        {
            throw new ArgumentException("Replay requires at least one message, event-type, or time selector.", nameof(request));
        }
    }
}
