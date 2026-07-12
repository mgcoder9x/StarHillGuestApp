namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Một message integration-event ĐẾN từ bus (transport-agnostic), sẵn sàng cho <see cref="IIntegrationEventDispatcher"/>
/// khử trùng + phân phối tới handler. Adapter (vd RabbitMQ) dựng DTO này từ delivery: <paramref name="MessageId"/>
/// = <c>IntegrationEvent.Id</c> (khoá idempotency, AD-029), <paramref name="EventType"/> = header/routing key
/// (registry resolve → CLR type), <paramref name="Payload"/> = body JSON thô (deserialize theo type đã resolve).
/// <para>
/// <b>Metadata envelope</b> (A-10): <see cref="ContentType"/> + <see cref="SchemaVersion"/> do producer phát kèm
/// (adapter đọc từ property/header, KHÔNG được vứt). Dispatcher validate <see cref="ContentType"/> == JSON TRƯỚC
/// khi deserialize (content-type lạ → quarantine). <see cref="SchemaVersion"/> mang theo cho versioning/diagnostics;
/// kiểm tương thích version theo từng EventType thuộc phạm vi khác (registry keyed theo version).
/// </para>
/// </summary>
/// <param name="MessageId">Định danh message = <c>IntegrationEvent.Id</c> (khoá inbox cùng <paramref name="Consumer"/>).</param>
/// <param name="Consumer">Tên consumer logic (một message có thể được nhiều consumer xử lý độc lập — PK inbox).</param>
/// <param name="EventType">Mã EventType ổn định (registry ánh xạ → CLR type; lạ → dead-letter).</param>
/// <param name="Payload">Body JSON thô (UTF-8) của event.</param>
public sealed record IncomingIntegrationMessage(
    Guid MessageId,
    string Consumer,
    string EventType,
    ReadOnlyMemory<byte> Payload)
{
    /// <summary>Content-type của payload (producer gắn). Dispatcher yêu cầu JSON (A-10). Null = không hợp lệ.</summary>
    public string? ContentType { get; init; }

    /// <summary>Phiên bản schema payload (producer gắn header). Phải >= 1 (dispatcher quarantine nếu &lt;1 — P1-02/AD-093).
    /// Compat theo từng EventType do mô hình "breaking → EventType mới + tolerant reader" đảm bảo (AD-083); registry
    /// key theo EventType (KHÔNG theo version).</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// W3C trace context (traceparent) của trace GỐC lúc enqueue (producer lưu <c>Activity.Current.Id</c>). Dispatcher
    /// parse → tạo consumer span là CON của trace gốc (trace xuyên bus — A-29/F34/F21). Null = không có context.
    /// </summary>
    public string? CorrelationId { get; init; }
}
