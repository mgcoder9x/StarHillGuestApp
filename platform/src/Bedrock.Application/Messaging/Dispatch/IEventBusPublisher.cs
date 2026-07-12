namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Cổng ra bus (port). Implement ở ADAPTER cụ thể (vd <c>Adapters.Messaging.RabbitMq</c>) — KHÔNG ở lõi,
/// KHÔNG lộ cho use case (CP11). Chỉ <see cref="IOutboxDispatcher"/> (worker) gọi. Nhận envelope BẤT BIẾN
/// <see cref="OutgoingIntegrationMessage"/> (chỉ payload thô + headers, KHÔNG lộ cột retry/persistence của
/// outbox — A-20/AD-081); adapter không cần biết CLR type (design §5.2).
/// </summary>
public interface IEventBusPublisher
{
    Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default);
}
