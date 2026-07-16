using Bedrock.Application.UseCases;
using Housekeeping.Domain;

namespace Housekeeping.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) cho các đọc CẦN sắp/lọc mà <c>IRepository</c> không làm
/// được: (a) ticket hiện hành của phòng (mới nhất — guest xem trạng thái, Req 6.7); (b) id các ticket MỞ của một visit
/// (cascade huỷ khi visit kết thúc — CP9); (c) board admin phân trang theo trạng thái (Req 6.3/9). Chỉ đọc (no-tracking).
/// Mirror EfFaqReader/IRoomQueries.
/// </summary>
public interface IHousekeepingReader
{
    /// <summary>Ticket MỚI NHẤT của phòng (mọi trạng thái) — <c>null</c> nếu phòng chưa từng có ticket.</summary>
    Task<HousekeepingTicketView?> GetCurrentTicketByRoomAsync(Guid roomId, CancellationToken ct = default);

    /// <summary>Id các ticket MỞ (Requested/InProgress) do một visit tạo — cho cascade huỷ (CP9).</summary>
    Task<IReadOnlyList<Guid>> ListOpenTicketIdsByVisitAsync(Guid guestVisitId, CancellationToken ct = default);

    /// <summary>Board ticket của resort (phân trang, lọc theo <paramref name="status"/> nếu có), sắp CŨ NHẤT trước (FIFO xử lý — Req 6.3/9).</summary>
    Task<PagedResult<HousekeepingBoardItem>> ListBoardAsync(
        Guid resortId, HousekeepingStatus? status, PagedRequest paging, CancellationToken ct = default);
}

/// <summary>Item board admin — đầy đủ metadata vận hành (ai hoàn tất, phương thức, mốc thời gian).</summary>
public sealed record HousekeepingBoardItem(
    Guid TicketId,
    Guid RoomId,
    Guid? GuestVisitId,
    HousekeepingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId,
    HousekeepingCompletionMethod? CompletionMethod);
