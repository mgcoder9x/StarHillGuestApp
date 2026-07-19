namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>Immutable snapshot of one dead-lettered message selected by a replay operation.</summary>
internal sealed class OutboxReplayAudit
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid OperationId { get; init; }

    public Guid MessageId { get; init; }

    public required string EventType { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset DeadLetteredAt { get; init; }

    public int ErrorCount { get; init; }

    public string? LastError { get; init; }

    public DateTimeOffset ReplayedAt { get; init; }
}
