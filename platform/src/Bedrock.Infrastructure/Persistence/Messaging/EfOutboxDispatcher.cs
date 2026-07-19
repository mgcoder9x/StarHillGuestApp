using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Worker phát Outbox (design §7.2, R8.4–8.6, CP15). Một lượt <see cref="DispatchPendingAsync"/>:
/// mở transaction → claim batch pending (chưa processed, chưa dead-letter, tới hạn <c>next_attempt_at</c>)
/// → publish qua <see cref="IEventBusPublisher"/> → mark <c>processed_at</c>; fail → <c>error_count</c>++ +
/// backoff (<c>next_attempt_at</c>), vượt <c>MaxAttempts</c> → <c>dead_lettered_at</c> (cách ly poison, KHÔNG
/// chặn message khác) → SaveChanges + COMMIT (nhả claim).
/// <para>
/// <b>Generic theo <typeparamref name="TContext"/></b>: mỗi module đăng ký dispatcher riêng cho DbContext của
/// mình (per-module — bảng outbox nằm trong schema module, design §4.6) với named-options độc lập.
/// </para>
/// <para>
/// <b>At-least-once</b>: crash sau <c>PublishAsync</c> trước khi mark → publish lại lượt sau (Inbox phía consumer
/// khử trùng → hiệu ứng đúng-một-lần nghiệp vụ). <b>Claim exclusive đa-instance</b> (Postgres
/// <c>FOR UPDATE SKIP LOCKED</c>) + test claim đồng thời thuộc task 7.4 (Testcontainers) — bản này claim
/// provider-agnostic (đúng single-instance; đa-instance vẫn an toàn nghiệp vụ nhờ Inbox).
/// </para>
/// </summary>
public sealed partial class EfOutboxDispatcher<TContext>(
    TContext context,
    IEventBusPublisher publisher,
    IClock clock,
    IOptionsMonitor<OutboxDispatcherOptions> optionsMonitor,
    ILogger<EfOutboxDispatcher<TContext>>? logger = null) : IOutboxDispatcher
    where TContext : PlatformDbContext
{
    private readonly ILogger _logger = logger ?? NullLogger<EfOutboxDispatcher<TContext>>.Instance;

    public async Task DispatchPendingAsync(CancellationToken ct = default)
    {
        var options = optionsMonitor.Get(OutboxDispatcherOptions.KeyFor<TContext>());
        var claimId = Guid.CreateVersion7();
        var now = clock.UtcNow;
        var batch = await ClaimBatchWithLeaseAsync(
            claimId,
            now,
            now + options.ClaimLease,
            options.BatchSize,
            ct).ConfigureAwait(false);

        // Publish ngoài DB transaction: broker chậm không giữ connection/row lock. Crash trước finalize → lease
        // hết hạn và message được phát lại (at-least-once, inbox dedupe).
        foreach (var message in batch)
        {
            await TryPublishAndFinalizeAsync(message, claimId, options, ct).ConfigureAwait(false);
        }

        await TryRefreshOperationalMetricsAsync(options, ct).ConfigureAwait(false);
    }

    private async Task<List<OutboxMessage>> ClaimBatchWithLeaseAsync(
        Guid claimId,
        DateTimeOffset now,
        DateTimeOffset claimedUntil,
        int batchSize,
        CancellationToken ct)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var batch = await ClaimBatchAsync(now, batchSize, ct).ConfigureAwait(false);
            foreach (var message in batch)
            {
                message.ClaimId = claimId;
                message.ClaimedUntil = claimedUntil;
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return batch;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Claim batch pending tới hạn. Điều kiện <c>processed_at IS NULL AND dead_lettered_at IS NULL</c> luôn ở
    /// SQL (khớp partial index <c>ix_outbox_pending</c>). Với Npgsql (production): lọc <c>next_attempt_at</c> +
    /// <c>ORDER BY occurred_at</c> cũng ở SQL. Provider khác (SQLite test): EF không dịch được so sánh/sắp xếp
    /// <c>DateTimeOffset</c> → làm phần đó client-side (tập pending nhỏ vì dispatcher poll thường xuyên).
    /// </summary>
    private async Task<List<OutboxMessage>> ClaimBatchAsync(DateTimeOffset now, int batchSize, CancellationToken ct)
    {
        if (context.Database.IsNpgsql())
        {
            // CP15 (design §4.5, R8.6): claim NGUYÊN TỬ đa-instance bằng FOR UPDATE SKIP LOCKED — dispatcher A
            // khoá row nó lấy; dispatcher B BỎ QUA row đang khoá (không chờ, không claim trùng) → KHÔNG double-publish.
            // EF LINQ không dịch được SKIP LOCKED → raw SQL. Tên bảng/schema lấy TỪ MODEL (không hardcode, an toàn
            // injection; now/batchSize là tham số). Row trả về vẫn được ChangeTracker theo dõi → set ProcessedAt persist.
            var sql = BuildNpgsqlClaimSql();
            return await context.Set<OutboxMessage>()
                .FromSqlRaw(sql, now, batchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        var candidates = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.DeadLetteredAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. candidates
            .Where(m => m.NextAttemptAt is null || m.NextAttemptAt <= now)
            .Where(m => m.ClaimedUntil is null || m.ClaimedUntil <= now)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)];
    }

    /// <summary>
    /// Build câu SELECT ... FOR UPDATE SKIP LOCKED cho Npgsql. Tên bảng/schema lấy từ model (khớp per-module
    /// schema + snake_case), quote an toàn; điều kiện pending khớp partial index <c>ix_outbox_pending</c>.
    /// </summary>
    private string BuildNpgsqlClaimSql()
    {
        var entityType = context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage chưa được map (thiếu modelBuilder.AddOutboxInbox()?).");
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException("OutboxMessage không có tên bảng.");
        var schema = entityType.GetSchema();
        var qualified = schema is null ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";

        return "SELECT * FROM " + qualified
            + " WHERE processed_at IS NULL AND dead_lettered_at IS NULL"
            + " AND (next_attempt_at IS NULL OR next_attempt_at <= {0})"
            + " AND (claimed_until IS NULL OR claimed_until <= {0})"
            + " ORDER BY occurred_at LIMIT {1} FOR UPDATE SKIP LOCKED";
    }

    private static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    /// <summary>Map record outbox (persistence, mutable) → envelope BẤT BIẾN hướng transport (A-20/AD-081).</summary>
    private static OutgoingIntegrationMessage ToOutgoing(OutboxMessage message) => new()
    {
        Id = message.Id,
        EventType = message.EventType,
        SchemaVersion = message.SchemaVersion,
        Payload = message.Payload,
        OccurredAt = message.OccurredAt,
        TraceParent = message.TraceParent, // P1-14: W3C trace context tách khỏi business correlation.
        TraceState = message.TraceState,
        CorrelationId = message.CorrelationId,
    };

    private async Task TryPublishAndFinalizeAsync(
        OutboxMessage message,
        Guid claimId,
        OutboxDispatcherOptions options,
        CancellationToken ct)
    {
#pragma warning disable CA1031 // Publish tới bus ngoài có thể ném BẤT KỲ (network/serialize/adapter). Phải bắt rộng để backoff/dead-letter và cách ly poison — không rethrow để message khác trong batch vẫn xử lý (design §7.2).
        try
        {
            // Batch được claim cùng lúc nhưng publish tuần tự. Gia hạn ngay trước từng message để ClaimLease chỉ cần
            // bao phủ một broker call, không phải worst-case của toàn batch.
            if (await TryRenewLeaseAsync(message.Id, claimId, options.ClaimLease, ct).ConfigureAwait(false) != 1)
            {
                RecordLeaseLost(message.Id, claimId, "renew");
                return;
            }

            // A-20: map record persistence → envelope BẤT BIẾN hướng transport (adapter không thấy cột retry/lease).
            await publisher.PublishAsync(ToOutgoing(message), ct).ConfigureAwait(false);
            var processedAt = clock.UtcNow;
            var affected = await context.Set<OutboxMessage>()
                .Where(candidate => candidate.Id == message.Id && candidate.ClaimId == claimId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(candidate => candidate.ProcessedAt, processedAt)
                        .SetProperty(candidate => candidate.ClaimId, (Guid?)null)
                        .SetProperty(candidate => candidate.ClaimedUntil, (DateTimeOffset?)null),
                    ct)
                .ConfigureAwait(false);
            if (affected == 1)
            {
                OutboxMetrics.RecordPublished(TelemetryName(options), processedAt - message.OccurredAt);
            }
            else
            {
                RecordLeaseLost(message.Id, claimId, "published-finalize");
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            var errorCount = message.ErrorCount + 1;
            var failedAt = clock.UtcNow;
            var lastError = OutboxErrorFormatter.Redact(ex); // P1-07/A-28: redact secret + classification + bound.
            if (errorCount >= options.MaxAttempts)
            {
                var affected = await context.Set<OutboxMessage>()
                    .Where(candidate => candidate.Id == message.Id && candidate.ClaimId == claimId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(candidate => candidate.ErrorCount, errorCount)
                            .SetProperty(candidate => candidate.DeadLetteredAt, failedAt)
                            .SetProperty(candidate => candidate.LastError, lastError)
                            .SetProperty(candidate => candidate.LastAttemptAt, failedAt)
                            .SetProperty(candidate => candidate.ClaimId, (Guid?)null)
                            .SetProperty(candidate => candidate.ClaimedUntil, (DateTimeOffset?)null),
                        ct)
                    .ConfigureAwait(false);
                if (affected == 1)
                {
                    OutboxMetrics.RecordDeadLettered(TelemetryName(options));
                }
                else
                {
                    RecordLeaseLost(message.Id, claimId, "dead-letter-finalize");
                }
            }
            else
            {
                var backoff = OutboxBackoff.ComputeBackoff(errorCount, options);

                // Jitter tất định theo Id (0..20%): trải tải chống thundering herd mà KHÔNG dùng RNG (test được).
                var jitterSeed = message.Id.GetHashCode() & int.MaxValue; // & MaxValue: tránh Math.Abs(int.MinValue) overflow.
                var jitterFraction = (jitterSeed % 1000) / 1000.0 * 0.2;
                var nextAttemptAt = failedAt + backoff + (backoff * jitterFraction);
                var affected = await context.Set<OutboxMessage>()
                    .Where(candidate => candidate.Id == message.Id && candidate.ClaimId == claimId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(candidate => candidate.ErrorCount, errorCount)
                            .SetProperty(candidate => candidate.NextAttemptAt, nextAttemptAt)
                            .SetProperty(candidate => candidate.LastError, lastError)
                            .SetProperty(candidate => candidate.LastAttemptAt, failedAt)
                            .SetProperty(candidate => candidate.ClaimId, (Guid?)null)
                            .SetProperty(candidate => candidate.ClaimedUntil, (DateTimeOffset?)null),
                    ct)
                    .ConfigureAwait(false);
                if (affected == 0)
                {
                    RecordLeaseLost(message.Id, claimId, "retry-finalize");
                }
            }
        }
#pragma warning restore CA1031
    }

    private Task<int> TryRenewLeaseAsync(
        Guid messageId,
        Guid claimId,
        TimeSpan claimLease,
        CancellationToken ct) =>
        context.Set<OutboxMessage>()
            .Where(candidate => candidate.Id == messageId && candidate.ClaimId == claimId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    candidate => candidate.ClaimedUntil,
                    clock.UtcNow + claimLease),
                ct);

    private void RecordLeaseLost(Guid messageId, Guid claimId, string phase)
    {
        var options = optionsMonitor.Get(OutboxDispatcherOptions.KeyFor<TContext>());
        OutboxMetrics.RecordLeaseLost(TelemetryName(options));
        Log.LeaseLost(_logger, messageId, claimId, phase);
    }

    private async Task TryRefreshOperationalMetricsAsync(OutboxDispatcherOptions options, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var telemetryName = TelemetryName(options);
        if (!OutboxMetricsRefreshState<TContext>.TryAcquire(
                telemetryName,
                now,
                options.OperationalMetricsRefreshInterval))
        {
            return;
        }

#pragma warning disable CA1031 // Telemetry must not break publishing; log and retry after the refresh interval.
        try
        {
            var snapshot = context.Database.IsNpgsql()
                ? await ReadNpgsqlOperationalSnapshotAsync(ct).ConfigureAwait(false)
                : await ReadProviderAgnosticOperationalSnapshotAsync(ct).ConfigureAwait(false);

            var oldestAge = snapshot.OldestPendingAt is null
                ? TimeSpan.Zero
                : now - snapshot.OldestPendingAt.Value;
            OutboxMetrics.RecordSnapshot(
                telemetryName,
                snapshot.PendingCount,
                oldestAge,
                snapshot.DeadLetterDepth);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Log.MetricsRefreshFailed(_logger, typeof(TContext).Name, ex);
        }
#pragma warning restore CA1031
    }

    private async Task<OperationalSnapshot> ReadNpgsqlOperationalSnapshotAsync(CancellationToken ct)
    {
        var snapshot = await context.Set<OutboxMessage>()
            .GroupBy(_ => 1)
            .Select(group => new OperationalSnapshot(
                group.LongCount(message => message.ProcessedAt == null && message.DeadLetteredAt == null),
                group
                    .Where(message => message.ProcessedAt == null && message.DeadLetteredAt == null)
                    .Min(message => (DateTimeOffset?)message.OccurredAt),
                group.LongCount(message => message.DeadLetteredAt != null)))
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return snapshot ?? new OperationalSnapshot(0, null, 0);
    }

    private async Task<OperationalSnapshot> ReadProviderAgnosticOperationalSnapshotAsync(CancellationToken ct)
    {
        // SQLite cannot translate Min/OrderBy for DateTimeOffset. Non-Npgsql providers are supported for tests/dev.
        var messages = await context.Set<OutboxMessage>()
            .Select(message => new { message.OccurredAt, message.ProcessedAt, message.DeadLetteredAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var pending = messages
            .Where(message => message.ProcessedAt is null && message.DeadLetteredAt is null)
            .ToArray();
        return new OperationalSnapshot(
            pending.LongLength,
            pending.Length == 0 ? null : pending.Min(message => message.OccurredAt),
            messages.LongCount(message => message.DeadLetteredAt is not null));
    }

    private static string TelemetryName(OutboxDispatcherOptions options) =>
        OutboxDispatcherOptions.TelemetryNameFor<TContext>(options);

    private sealed record OperationalSnapshot(
        long PendingCount,
        DateTimeOffset? OldestPendingAt,
        long DeadLetterDepth);

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message = "Outbox lease ownership lost (messageId={MessageId}, claimId={ClaimId}, phase={Phase}).")]
        public static partial void LeaseLost(ILogger logger, Guid messageId, Guid claimId, string phase);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Warning,
            Message = "Outbox operational metrics refresh failed for context {ContextName}.")]
        public static partial void MetricsRefreshFailed(ILogger logger, string contextName, Exception exception);
    }
}
