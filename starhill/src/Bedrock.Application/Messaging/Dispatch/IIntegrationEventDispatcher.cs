namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Phân phối một <see cref="IncomingIntegrationMessage"/> tới handler với đảm bảo idempotent (design §7.3, F30) —
/// PHÍA CONSUME, đối xứng với <see cref="IOutboxDispatcher"/> phía publish. Đây là CƠ CHẾ agnostic (không biết
/// transport): registry resolve EventType → CLR type → mở transaction → <c>IInboxStore.TryMarkProcessedAsync</c>
/// → nếu lần đầu gọi mọi <c>IIntegrationEventHandler&lt;T&gt;</c> → COMMIT (inbox + business nguyên tử).
/// <para>
/// Ở namespace <c>Messaging.Dispatch</c> (worker-side) → use case KHÔNG được phụ thuộc (CP11); adapter transport
/// (vd RabbitMQ) phụ thuộc port này (KHÔNG phụ thuộc impl ở Infrastructure — giữ CP3). Impl:
/// <c>Bedrock.Infrastructure...EfIntegrationEventDispatcher</c>.
/// </para>
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Xử lý một message. Trả <see cref="InboxDispatchOutcome"/> cho các trường hợp ACK (Handled/Duplicate/DeadLettered).
    /// NÉM exception nếu handler lỗi (transaction rollback) → transport NACK để redeliver (inbox khử trùng lần sau).
    /// </summary>
    Task<InboxDispatchOutcome> DispatchAsync(IncomingIntegrationMessage message, CancellationToken ct = default);
}
