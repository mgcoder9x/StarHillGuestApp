namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Chống xử lý trùng ở phía CONSUMER (inbox/idempotency, F30). Kết hợp at-least-once publish (outbox) +
/// idempotent consume (inbox) = hiệu ứng đúng-một-lần về nghiệp vụ.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Cố đánh dấu (messageId, consumer) là đã xử lý. Trả <c>true</c> nếu LẦN ĐẦU (được phép chạy handler),
    /// <c>false</c> nếu đã xử lý trước đó (bỏ qua). Nên nằm trong cùng transaction với business của handler.
    /// </summary>
    Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default);
}
