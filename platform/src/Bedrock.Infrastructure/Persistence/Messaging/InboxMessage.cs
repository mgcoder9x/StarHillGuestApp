namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Bản ghi Inbox (idempotency phía CONSUMER — F30, design §4.5/§7.3). Persistence-facing, GIẤU trong
/// Infrastructure (không lộ ra Application — cùng triết lý ẩn <c>RefreshTokenRecord</c>, F19). Khoá chính
/// tổ hợp <c>(MessageId, Consumer)</c>: cùng một message giao cho cùng một consumer chỉ xử lý một lần.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>Định danh message (= <c>IntegrationEvent.Id</c>, chảy qua outbox → bus → consumer).</summary>
    public required Guid MessageId { get; init; }

    /// <summary>Tên consumer (một message có thể được nhiều consumer khác nhau xử lý độc lập).</summary>
    public required string Consumer { get; init; }

    public DateTimeOffset ProcessedAt { get; set; }
}
