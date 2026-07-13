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

    /// <summary>Lease owner của dispatcher. Null = chưa được claim hoặc đã finalize.</summary>
    public Guid? ClaimId { get; set; }

    /// <summary>Lease hết hạn để instance khác recovery sau crash.</summary>
    public DateTimeOffset? ClaimedUntil { get; set; }

    /// <summary>
    /// Thông điệp lỗi publish gần nhất (P1-07: đã REDACT credential/token + classification <c>KiểuException: message</c>,
    /// cắt tối đa <see cref="MaxLastErrorLength"/> ký tự) — chẩn đoán operability (A-28: "vì sao event này fail"). Null = chưa lỗi.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>Thời điểm lần thử publish THẤT BẠI gần nhất (đi kèm <see cref="LastError"/>). Null = chưa lỗi.</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>
    /// P1-14: BUSINESS correlation id (đối soát nghiệp vụ, độc lập trace). Null nếu chưa có nguồn (hiện tại —
    /// business-correlation port là follow-up). KHÔNG còn chứa traceparent (đã tách sang <see cref="TraceParent"/>).
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>P1-14: W3C <c>traceparent</c> của trace gốc lúc enqueue (distributed tracing xuyên bus). Null = không có.</summary>
    public string? TraceParent { get; init; }

    /// <summary>P1-14: W3C <c>tracestate</c> (vendor sampling/state) — giữ để không mất khi propagate. Null = không có.</summary>
    public string? TraceState { get; init; }

    /// <summary>Trần độ dài <see cref="LastError"/> (bound chống text vô hạn + giới hạn rò rỉ — A-28).</summary>
    public const int MaxLastErrorLength = 1024;

    /// <summary>Trần độ dài <see cref="TraceParent"/> (W3C traceparent ~55 ký tự; để 512 an toàn).</summary>
    public const int MaxTraceParentLength = 512;

    /// <summary>Trần độ dài <see cref="TraceState"/> (W3C tracestate có thể dài — bound 1024).</summary>
    public const int MaxTraceStateLength = 1024;
}
