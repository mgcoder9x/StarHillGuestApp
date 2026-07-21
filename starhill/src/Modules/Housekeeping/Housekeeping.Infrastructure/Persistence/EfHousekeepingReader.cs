using Bedrock.Application.UseCases;
using Housekeeping.Application;
using Housekeeping.Domain;
using Microsoft.EntityFrameworkCore;

namespace Housekeeping.Infrastructure.Persistence;

/// <summary>
/// Impl EF read-model <see cref="IHousekeepingReader"/> (no-tracking). <see cref="GetCurrentTicketByRoomAsync"/>:
/// ticket mới nhất của phòng (CreatedAt desc — guest xem trạng thái Req 6.7). <see cref="ListOpenTicketIdsByVisitAsync"/>:
/// id ticket MỞ do visit tạo (cascade huỷ CP9). Mirror EfFaqReader.
/// </summary>
public sealed class EfHousekeepingReader(HousekeepingDbContext db) : IHousekeepingReader
{
    public async Task<HousekeepingTicketView?> GetCurrentTicketByRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        // Order theo Id DESC (UUIDv7 — Id time-ordered by design vì Entity sinh Guid.CreateVersion7) = "mới nhất"
        // TƯƠNG ĐƯƠNG CreatedAt DESC nhưng PROVIDER-AGNOSTIC: SQLite KHÔNG hỗ trợ ORDER BY DateTimeOffset (chỉ Postgres).
        // Bản chất: không để giới hạn provider test (SQLite) buộc order client-side unbounded; Id-v7 là nguồn thứ tự hợp lệ.
        return await db.HousekeepingTickets
            .AsNoTracking()
            .Where(t => t.RoomId == roomId)
            .OrderByDescending(t => t.Id)
            .Select(t => new HousekeepingTicketView(
                t.Id, t.RoomId, t.Status, t.CreatedAt, t.StartedAt, t.CompletedAt,
                t.ServiceType, t.PreferredTime, t.PreferredTimeText,
                t.AmenityToothbrush, t.AmenityTowel, t.AmenityWater, t.AmenitySoap, t.Note))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> ListOpenTicketIdsByVisitAsync(
        Guid guestVisitId, CancellationToken ct = default)
    {
        return await db.HousekeepingTickets
            .AsNoTracking()
            .Where(t => t.GuestVisitId == guestVisitId
                        && (t.Status == HousekeepingStatus.Requested || t.Status == HousekeepingStatus.InProgress))
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<HousekeepingBoardItem>> ListBoardAsync(
        Guid resortId, HousekeepingStatus? status, PagedRequest paging, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paging);

        var query = db.HousekeepingTickets.AsNoTracking().Where(t => t.ResortId == resortId);
        if (status is not null)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var total = await query.LongCountAsync(ct).ConfigureAwait(false);

        // Order theo Id ASC (UUIDv7 time-ordered) = CŨ NHẤT trước (FIFO staff xử lý). Provider-agnostic (SQLite KHÔNG
        // ORDER BY DateTimeOffset). Id-v7 tương đương CreatedAt asc.
        var items = await query
            .OrderBy(t => t.Id)
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .Select(t => new HousekeepingBoardItem(
                t.Id, t.RoomId, t.GuestVisitId, t.Status, t.CreatedAt, t.StartedAt, t.CompletedAt,
                t.CompletedByUserId, t.CompletionMethod,
                t.ServiceType, t.PreferredTime, t.PreferredTimeText,
                t.AmenityToothbrush, t.AmenityTowel, t.AmenityWater, t.AmenitySoap, t.Note))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<HousekeepingBoardItem>(items, paging.SafePage, paging.SafePageSize, total);
    }
}
