namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Kết quả một lần <see cref="IIntegrationEventDispatcher.DispatchAsync"/> — để adapter transport quyết định
/// ACK/NACK. LƯU Ý: lỗi handler (transient/poison) KHÔNG trả qua enum này mà NÉM exception (transaction rollback)
/// → adapter nack. Ba giá trị dưới đều là trường hợp "đã xử lý xong, ACK":
/// </summary>
public enum InboxDispatchOutcome
{
    /// <summary>Lần đầu: handler đã chạy + inbox mark commit cùng transaction (đúng-một-lần). → ACK.</summary>
    Handled,

    /// <summary>Đã xử lý trước đó (inbox trùng) → bỏ qua idempotent (không chạy handler lại). → ACK.</summary>
    Duplicate,

    /// <summary>EventType không có trong registry → không deserialize/handle được; cách ly, KHÔNG crash (R17.3). → ACK (drop/DLX).</summary>
    DeadLettered,
}
