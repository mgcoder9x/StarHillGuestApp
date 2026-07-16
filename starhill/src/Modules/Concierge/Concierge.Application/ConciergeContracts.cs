using Concierge.Domain;

namespace Concierge.Application;

// ---- Guest: gửi tin (create/reopen 1-hội-thoại/visit) + đọc hội thoại của mình (K-Con.2a, Req 5) ----
// ResortId/RoomId/GuestSessionId/GuestVisitId do server phân giải từ ICurrentGuestContextResolver (Api). KHÔNG tin client.

/// <summary>Khách gửi tin. Tạo hội thoại cho visit nếu chưa có; nếu đã có (kể cả Closed mà visit còn hiệu lực) →
/// append + mở lại (reopen). Body PLAIN TEXT (trim + giới hạn MaxMessageLength ở use case, KHÔNG sanitize).</summary>
public sealed record SendGuestMessageInput(Guid ResortId, Guid RoomId, Guid GuestSessionId, Guid GuestVisitId, string Body);

/// <summary>Kết quả gửi: hội thoại + tin vừa tạo + <see cref="Reopened"/> (true nếu hội thoại đang Closed được mở lại).</summary>
public sealed record SendGuestMessageResult(Guid ConversationId, Guid MessageId, ConversationStatus Status, bool Reopened);

/// <summary>Khách đọc hội thoại của LƯỢT LƯU TRÚ hiện tại của mình (Req 5.9 — scope theo visit). Polling fallback.</summary>
public sealed record GetGuestConversationInput(Guid GuestVisitId);

/// <summary>Kết quả đọc: hội thoại của visit (null nếu visit chưa từng mở hội thoại).</summary>
public sealed record GetGuestConversationResult(GuestConversationView? Conversation);

/// <summary>View hội thoại guest-facing: trạng thái + thời điểm tin gần nhất + danh sách tin (sắp theo thời gian).</summary>
public sealed record GuestConversationView(
    Guid ConversationId,
    ConversationStatus Status,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<GuestMessageView> Messages);

/// <summary>View tin guest-facing: gồm <see cref="ReadByStaffAt"/> để khách thấy "đã xem" cho tin của mình.
/// KHÔNG lộ SenderUserId (danh tính nhân viên nội bộ).</summary>
public sealed record GuestMessageView(
    Guid MessageId,
    MessageSenderType SenderType,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadByStaffAt);
