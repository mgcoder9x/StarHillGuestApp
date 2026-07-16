namespace Housekeeping.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) cho các đọc CẦN sắp/lọc mà <c>IRepository</c> không làm
/// được: (a) ticket hiện hành của phòng (mới nhất theo CreatedAt — guest xem trạng thái, Req 6.7); (b) id các ticket
/// MỞ của một visit (cascade huỷ khi visit kết thúc — CP9). Chỉ đọc (no-tracking). Board admin (phân trang) thêm ở H-Hk.3.
/// Mirror EfFaqReader/EfRulePublicationReader.
/// </summary>
public interface IHousekeepingReader
{
    /// <summary>Ticket MỚI NHẤT của phòng (mọi trạng thái) — <c>null</c> nếu phòng chưa từng có ticket.</summary>
    Task<HousekeepingTicketView?> GetCurrentTicketByRoomAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>Id các ticket MỞ (Requested/InProgress) do một visit tạo — cho cascade huỷ (CP9).</summary>
    Task<IReadOnlyList<Guid>> ListOpenTicketIdsByVisitAsync(Guid guestVisitId, CancellationToken ct = default);
}
