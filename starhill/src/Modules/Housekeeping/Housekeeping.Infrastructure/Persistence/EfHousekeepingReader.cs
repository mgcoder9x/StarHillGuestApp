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
                t.Id, t.RoomId, t.Status, t.CreatedAt, t.StartedAt, t.CompletedAt))
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
}
