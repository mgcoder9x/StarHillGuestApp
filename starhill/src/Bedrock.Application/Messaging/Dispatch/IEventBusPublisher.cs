namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Cổng ra bus (port). Implement ở ADAPTER cụ thể (vd <c>Adapters.Messaging.RabbitMq</c>) — KHÔNG ở lõi,
/// KHÔNG lộ cho use case (CP11). Chỉ <see cref="IOutboxDispatcher"/> (worker) gọi. Publish payload thô +
/// headers (adapter không cần biết CLR type — design §5.2).
/// </summary>
public interface IEventBusPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct = default);
}
