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

// ---- Staff/admin: reply, mark-read, close, cascade close (K-Con.2b, Req 5.3/5.4/10.8) ----
// Actor (StaffUserId) do Api phân giải từ ICurrentUser (endpoint RequireStaff) rồi truyền vào — KHÔNG tin client (mirror QR-DV-004).

/// <summary>Nhân viên trả lời hội thoại. Append tin(Staff, SenderUserId=StaffUserId); mở lại nếu đang Closed. Body PLAIN TEXT.</summary>
public sealed record ReplyConversationInput(Guid ConversationId, Guid StaffUserId, string Body);

public sealed record ReplyConversationResult(Guid MessageId, ConversationStatus Status);

/// <summary>Nhân viên đánh dấu ĐÃ ĐỌC hội thoại: UnreadForStaff=0 + ReadByStaffAt cho các tin khách chưa đọc.</summary>
public sealed record MarkConversationReadInput(Guid ConversationId);

/// <summary>Nhân viên đóng hội thoại (ClosedByUserId=StaffUserId).</summary>
public sealed record CloseConversationInput(Guid ConversationId, Guid StaffUserId);

/// <summary>Đóng hội thoại Open của một visit khi visit kết thúc (System — cascade CP9/C-GA.5; ClosedByUserId=null). Idempotent.</summary>
public sealed record CloseConversationForVisitInput(Guid GuestVisitId);

// ---- Internal notes CRUD (K-Con.2b, Req 9.4) — chỉ admin RequireStaff, KHÔNG lộ guest ----

/// <summary>Tạo ghi chú nội bộ: gắn phòng (RoomId) và/hoặc hội thoại (ConversationId); ít nhất một. AuthorUserId do Api cấp.</summary>
public sealed record CreateInternalNoteInput(Guid ResortId, Guid? RoomId, Guid? ConversationId, Guid AuthorUserId, string Body);

public sealed record CreateInternalNoteResult(Guid NoteId);

/// <summary>Sửa nội dung ghi chú (concurrency xmin — CP15).</summary>
public sealed record UpdateInternalNoteInput(Guid NoteId, string Body);

/// <summary>Xóa ghi chú nội bộ.</summary>
public sealed record DeleteInternalNoteInput(Guid NoteId);

// ---- Admin board read-model (K-Con.2b, Req 5.3) — nhân viên xem danh sách + chi tiết hội thoại ----

/// <summary>Tóm tắt hội thoại cho board (danh sách theo hoạt động gần nhất). RoomId để nhân viên biết phòng nào.</summary>
public sealed record ConversationSummary(
    Guid ConversationId,
    Guid RoomId,
    ConversationStatus Status,
    DateTimeOffset LastMessageAt,
    int UnreadForStaff);

/// <summary>Trang danh sách hội thoại (paged) — board dashboard.</summary>
public sealed record PagedConversations(IReadOnlyList<ConversationSummary> Items, int Page, int PageSize, int TotalCount);

/// <summary>View tin staff-facing: LỘ SenderUserId (nội bộ) + cả hai mốc đọc.</summary>
public sealed record StaffMessageView(
    Guid MessageId,
    MessageSenderType SenderType,
    Guid? SenderUserId,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadByStaffAt,
    DateTimeOffset? ReadByGuestAt);

/// <summary>Chi tiết hội thoại cho nhân viên (gồm RoomId/GuestVisitId + tin staff-facing).</summary>
public sealed record ConversationDetailView(
    Guid ConversationId,
    Guid RoomId,
    Guid GuestVisitId,
    ConversationStatus Status,
    DateTimeOffset LastMessageAt,
    int UnreadForStaff,
    IReadOnlyList<StaffMessageView> Messages);
