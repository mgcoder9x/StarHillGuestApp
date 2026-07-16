namespace Concierge.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) cho các đọc CẦN sắp/lọc mà <c>IRepository</c> không làm
/// được: (a) hội thoại của một visit + danh sách tin sắp theo thời gian (guest đọc — Req 5.9); (b) id các tin của
/// nhân viên mà khách CHƯA đọc (để đánh dấu ReadByGuest khi khách mở hội thoại). Chỉ đọc (no-tracking). Sắp theo Id
/// (UUIDv7 time-ordered) — PROVIDER-AGNOSTIC (SQLite KHÔNG ORDER BY DateTimeOffset — bài học QR-N-059). Mirror EfHousekeepingReader.
/// </summary>
public interface IConciergeReader
{
    /// <summary>Hội thoại của visit + tin (sắp CŨ→MỚI theo Id-v7) — <c>null</c> nếu visit chưa từng mở hội thoại.</summary>
    Task<GuestConversationView?> GetGuestConversationByVisitAsync(Guid guestVisitId, CancellationToken ct = default);

    /// <summary>Id các tin do nhân viên gửi (<c>SenderType=Staff</c>) mà khách CHƯA đọc (<c>ReadByGuestAt=null</c>) trong hội thoại.</summary>
    Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByGuestAsync(Guid conversationId, CancellationToken ct = default);
}
