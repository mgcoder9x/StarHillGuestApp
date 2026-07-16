using Bedrock.Domain.Entities;

namespace Concierge.Domain;

/// <summary>
/// Hội thoại chat khách↔lễ tân, gom theo lượt lưu trú (Req 5). ĐÚNG MỘT hội thoại mỗi <see cref="GuestVisitId"/> —
/// ràng buộc unique <c>ux_conversation_visit</c> (Req 5.2). Khi khách gửi lại sau khi hội thoại đã <see cref="ConversationStatus.Closed"/>
/// mà visit CÒN hiệu lực → MỞ LẠI đúng hội thoại này (Status→Open), KHÔNG tạo hội thoại mới (Req 5.10). Mọi Id
/// (<see cref="ResortId"/>/<see cref="RoomId"/>/<see cref="GuestSessionId"/>/<see cref="GuestVisitId"/>/
/// <see cref="ClosedByUserId"/>) là Guid TRẦN — KHÔNG FK chéo-schema (QR-AD-002). <see cref="UnreadForStaff"/> đếm
/// denormalize (khách gửi → ++; nhân viên đọc → 0) cho badge dashboard (Req 5.3). Concurrency token (xmin) chống
/// nhân viên đọc/đóng đồng thời khách gửi đè âm thầm (CP15).
/// </summary>
public sealed class Conversation : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    public required Guid RoomId { get; set; }

    /// <summary>Guest session mở hội thoại (Req 5.9 — scope theo session/visit hiện tại).</summary>
    public required Guid GuestSessionId { get; set; }

    /// <summary>Lượt lưu trú sở hữu hội thoại (unique). Cascade đóng khi visit kết thúc (CP9/C-GA.5).</summary>
    public required Guid GuestVisitId { get; set; }

    public ConversationStatus Status { get; set; }

    /// <summary>Thời điểm tin gần nhất (bất kỳ phía) — sắp xếp board dashboard.</summary>
    public DateTimeOffset LastMessageAt { get; set; }

    public DateTimeOffset? LastGuestMessageAt { get; set; }

    public DateTimeOffset? LastStaffMessageAt { get; set; }

    /// <summary>Số tin khách chưa được nhân viên đọc (denormalize — Req 5.3). Khách gửi → ++; nhân viên đọc → 0.</summary>
    public int UnreadForStaff { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Nhân viên đóng hội thoại (Guid trần — không FK identity; null nếu System đóng theo cascade).</summary>
    public Guid? ClosedByUserId { get; set; }

    public uint RowVersion { get; set; }
}
