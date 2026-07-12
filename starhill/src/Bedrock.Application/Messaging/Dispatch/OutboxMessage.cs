namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Bản ghi Outbox (transactional outbox pattern). POCO thuần (không attribute EF) — EF map bằng Fluent API
/// ở Infrastructure (task 7). Ghi CÙNG transaction với thay đổi state (F5/F25) → at-least-once publish
/// không lệch dữ liệu/event. DB là source-of-truth; bus chỉ là kênh. Schema: design §4.5.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Tên loại sự kiện ổn định (<c>IntegrationEvent.EventType</c>).</summary>
    public required string EventType { get; init; }

    public int SchemaVersion { get; init; } = 1;

    /// <summary>Payload đã serialize (System.Text.Json, options cố định — design §5.2).</summary>
    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Null = chưa publish thành công.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Số lần thử publish thất bại (retry/backoff).</summary>
    public int ErrorCount { get; set; }

    /// <summary>Null = đủ điều kiện publish ngay; set theo exponential backoff khi fail (R8.5/AD-016).</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>Khác null = dead-letter (cách ly, không retry tự động — AD-003/AD-016).</summary>
    public DateTimeOffset? DeadLetteredAt { get; set; }

    /// <summary>Correlation/trace để đối soát xuyên suốt (F34/F21).</summary>
    public string? CorrelationId { get; init; }
}
