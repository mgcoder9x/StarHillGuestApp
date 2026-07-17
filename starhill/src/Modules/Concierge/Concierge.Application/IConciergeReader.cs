using Concierge.Domain;

namespace Concierge.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) cho các đọc CẦN sắp/lọc mà <c>IRepository</c> không làm
/// được: (a) hội thoại của một visit + danh sách tin sắp theo thời gian (guest đọc — Req 5.9); (b) id các tin của
/// nhân viên mà khách CHƯA đọc (để đánh dấu ReadByGuest khi khách mở hội thoại); (c) board hội thoại của nhân viên
/// (Req 5.3) + chi tiết staff-facing; (d) id các tin của khách mà nhân viên CHƯA đọc (mark-read). Chỉ đọc (no-tracking).
/// Board sắp theo <c>LastMessageAt</c> DESC (mới-hoạt-động-nhất trước — nghiệp vụ dashboard); vì SQLite KHÔNG ORDER BY
/// DateTimeOffset (bài học QR-N-059) nên board đo trên Postgres. Tin trong hội thoại sắp CŨ→MỚI theo Id (UUIDv7 time-ordered,
/// provider-agnostic). Mirror EfHousekeepingReader.
/// </summary>
public interface IConciergeReader
{
    /// <summary>Hội thoại của visit + tin (sắp CŨ→MỚI theo Id-v7) — <c>null</c> nếu visit chưa từng mở hội thoại.</summary>
    Task<GuestConversationView?> GetGuestConversationByVisitAsync(Guid guestVisitId, CancellationToken ct = default);

    /// <summary>Id các tin do nhân viên gửi (<c>SenderType=Staff</c>) mà khách CHƯA đọc (<c>ReadByGuestAt=null</c>) trong hội thoại.</summary>
    Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByGuestAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>Id các tin do KHÁCH gửi (<c>SenderType=Guest</c>) mà nhân viên CHƯA đọc (<c>ReadByStaffAt=null</c>) trong hội thoại (để mark-read).</summary>
    Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByStaffAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>Board hội thoại của resort (Req 5.3), lọc theo <paramref name="status"/> (null = tất cả), phân trang,
    /// sắp <c>LastMessageAt</c> DESC (mới-hoạt-động-nhất trước).</summary>
    Task<PagedConversations> ListConversationsAsync(
        Guid resortId, ConversationStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Chi tiết hội thoại staff-facing (gồm RoomId/GuestVisitId + tin sắp CŨ→MỚI theo Id-v7 + SenderUserId) — <c>null</c> nếu không tồn tại.</summary>
    Task<ConversationDetailView?> GetConversationAsync(Guid conversationId, CancellationToken ct = default);
}
