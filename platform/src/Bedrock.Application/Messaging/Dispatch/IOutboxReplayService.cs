namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Operator request for inspecting or replaying quarantined outbox messages. At least one selector is required so a
/// malformed command cannot replay the entire dead-letter set. Authorization remains a Host/API policy; this contract
/// requires an explicit actor, reason and operation id for durable audit.
/// </summary>
public sealed record OutboxReplayRequest
{
    public required Guid OperationId { get; init; }

    public required string Actor { get; init; }

    public required string Reason { get; init; }

    public IReadOnlyCollection<Guid> MessageIds { get; init; } = [];

    public string? EventType { get; init; }

    public DateTimeOffset? OccurredFrom { get; init; }

    public DateTimeOffset? OccurredTo { get; init; }

    public int MaxMessages { get; init; } = 100;

    public bool DryRun { get; init; } = true;
}

public sealed record OutboxReplayCandidate(
    Guid MessageId,
    string EventType,
    DateTimeOffset OccurredAt,
    DateTimeOffset DeadLetteredAt,
    int ErrorCount,
    string? LastError);

public sealed record OutboxReplayResult(
    Guid OperationId,
    bool DryRun,
    IReadOnlyList<OutboxReplayCandidate> Candidates,
    int ReplayedCount);

public sealed record OutboxReplayAuditMessage(
    Guid MessageId,
    string EventType,
    DateTimeOffset OccurredAt,
    DateTimeOffset DeadLetteredAt,
    int ErrorCount,
    string? LastError,
    DateTimeOffset ReplayedAt);

public sealed record OutboxReplayAuditRecord(
    Guid OperationId,
    string Actor,
    string Reason,
    DateTimeOffset ReplayedAt,
    int ReplayedCount,
    IReadOnlyList<OutboxReplayAuditMessage> Messages);

public interface IOutboxReplayService
{
    Task<OutboxReplayResult> ExecuteAsync(OutboxReplayRequest request, CancellationToken ct = default);
}

/// <summary>Read-only lookup for reviewing a durable replay operation and its immutable message snapshots.</summary>
public interface IOutboxReplayAuditReader
{
    Task<OutboxReplayAuditRecord?> GetAsync(Guid operationId, CancellationToken ct = default);
}
