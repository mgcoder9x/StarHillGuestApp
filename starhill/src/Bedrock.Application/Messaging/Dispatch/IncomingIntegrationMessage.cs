namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Một message integration-event ĐẾN từ bus (transport-agnostic), sẵn sàng cho <see cref="IIntegrationEventDispatcher"/>
/// khử trùng + phân phối tới handler. Adapter (vd RabbitMQ) dựng DTO này từ delivery: <paramref name="MessageId"/>
/// = <c>IntegrationEvent.Id</c> (khoá idempotency, AD-029), <paramref name="EventType"/> = header/routing key
/// (registry resolve → CLR type), <paramref name="Payload"/> = body JSON thô (deserialize theo type đã resolve).
/// </summary>
/// <param name="MessageId">Định danh message = <c>IntegrationEvent.Id</c> (khoá inbox cùng <paramref name="Consumer"/>).</param>
/// <param name="Consumer">Tên consumer logic (một message có thể được nhiều consumer xử lý độc lập — PK inbox).</param>
/// <param name="EventType">Mã EventType ổn định (registry ánh xạ → CLR type; lạ → dead-letter).</param>
/// <param name="Payload">Body JSON thô (UTF-8) của event.</param>
public sealed record IncomingIntegrationMessage(
    Guid MessageId,
    string Consumer,
    string EventType,
    ReadOnlyMemory<byte> Payload);
