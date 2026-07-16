using Bedrock.Domain.Entities;

namespace Concierge.Domain;

/// <summary>
/// Một tin nhắn trong hội thoại (Req 5). Append-only (bất biến sau khi tạo, KHÔNG concurrency token). FK nội-module
/// <see cref="ConversationId"/>→<see cref="Conversation"/> (Cascade). <see cref="SenderUserId"/> Guid trần (không FK
/// identity; null khi Guest/System). <see cref="Body"/> lưu PLAIN TEXT — trim + giới hạn theo cấu hình
/// <c>MaxMessageLength</c> ở use case, KHÔNG HTML-sanitize (khác Rules/FAQ): chat là văn bản; client render bằng
/// <c>textContent</c> (tự escape), KHÔNG <c>innerHTML</c>. <see cref="ReadByStaffAt"/>/<see cref="ReadByGuestAt"/> đánh
/// dấu thời điểm đọc để tính chưa-đọc + hiển thị "đã xem".
/// </summary>
public sealed class Message : Entity
{
    public required Guid ConversationId { get; set; }

    public MessageSenderType SenderType { get; set; }

    /// <summary>Nhân viên gửi (null nếu Guest/System).</summary>
    public Guid? SenderUserId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReadByStaffAt { get; set; }

    public DateTimeOffset? ReadByGuestAt { get; set; }
}
