namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Envelope BẤT BIẾN hướng transport (A-20). Là hợp đồng DUY NHẤT mà <see cref="IEventBusPublisher"/> (adapter
/// bus pluggable — F29) nhìn thấy: chỉ dữ liệu "gửi đi" (id/type/version/payload/occurred/trace), KHÔNG lộ
/// các cột retry/persistence mutable của <see cref="OutboxMessage"/> (ProcessedAt/ErrorCount/NextAttemptAt/
/// DeadLetteredAt/ClaimId/ClaimedUntil). Adapter không cần và KHÔNG NÊN biết trạng thái outbox nội bộ.
/// <para>
/// Đối xứng với <c>IncomingIntegrationMessage</c> (phía consume). <c>EfOutboxDispatcher</c>
/// map <see cref="OutboxMessage"/> (record persistence) → envelope này ngay trước khi publish.
/// </para>
/// </summary>
public sealed record OutgoingIntegrationMessage
{
    /// <summary>Id envelope = <c>OutboxMessage.Id</c> = <c>IntegrationEvent.Id</c> (AD-029, idempotency đầu-cuối).</summary>
    public required Guid Id { get; init; }

    /// <summary>Tên loại sự kiện ổn định (routing key + header event-type).</summary>
    public required string EventType { get; init; }

    /// <summary>Phiên bản schema payload (header schema-version, versioning F32).</summary>
    public required int SchemaVersion { get; init; }

    /// <summary>Payload đã serialize (hợp đồng cố định camelCase/bỏ null — design §5.2).</summary>
    public required string Payload { get; init; }

    /// <summary>Thời điểm event xảy ra (envelope metadata, đo latency publish).</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// P1-14: W3C <c>traceparent</c> của trace GỐC lúc enqueue (distributed tracing — F34/F21). Đi qua header
    /// <c>traceparent</c> trên bus; consumer dựng span con. Null = không có trace lúc enqueue.
    /// </summary>
    public string? TraceParent { get; init; }

    /// <summary>P1-14: W3C <c>tracestate</c> (vendor sampling/state) đi kèm traceparent — TRƯỚC ĐÂY BỊ VỨT. Null = không có.</summary>
    public string? TraceState { get; init; }

    /// <summary>
    /// P1-14: BUSINESS correlation id (đối soát nghiệp vụ xuyên log/service), ĐỘC LẬP với trace context. Null nếu
    /// chưa có nguồn business-correlation (hiện tại — business-correlation port là follow-up). KHÔNG chứa traceparent.
    /// </summary>
    public string? CorrelationId { get; init; }
}
