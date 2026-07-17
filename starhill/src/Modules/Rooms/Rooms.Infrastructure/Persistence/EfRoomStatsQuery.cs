using Microsoft.EntityFrameworkCore;
using Rooms.Contracts;
using Rooms.Domain;

namespace Rooms.Infrastructure.Persistence;

/// <summary>
/// Impl EF <see cref="IRoomStatsQuery"/> (no-tracking, đọc-đếm) cho Host Dashboard. Phòng ACTIVE = Status=Active;
/// phòng xóa-mềm tự loại bởi global query filter ISoftDeletable (PlatformDbContext). Scope theo resort.
/// </summary>
public sealed class EfRoomStatsQuery(RoomsDbContext db) : IRoomStatsQuery
{
    public Task<int> CountActiveRoomsAsync(Guid resortId, CancellationToken ct = default) =>
        db.Rooms
            .AsNoTracking()
            .CountAsync(r => r.ResortId == resortId && r.Status == RoomStatus.Active, ct);
}
