namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>One durable operator command. The primary key makes an operation id single-use.</summary>
internal sealed class OutboxReplayOperation
{
    public const int MaxActorLength = 200;

    public const int MaxReasonLength = 2000;

    public Guid OperationId { get; init; }

    public required string Actor { get; init; }

    public required string Reason { get; init; }

    public DateTimeOffset ReplayedAt { get; init; }

    public int ReplayedCount { get; set; }
}
