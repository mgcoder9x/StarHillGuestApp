using Microsoft.EntityFrameworkCore;
using Rooms.Contracts;
using Rooms.Domain;

namespace Rooms.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IRoomTokenResolver"/> (read-only). Tra token Status=Active (token Revoked/không tồn tại
/// → null). Phòng đã xóa mềm bị query filter (ISoftDeletable) loại → null (KHÔNG lộ phòng khác — CP1). Đăng ký
/// scoped ở <c>AddRoomsInfrastructure</c>.
/// </summary>
public sealed class EfRoomTokenResolver(RoomsDbContext db) : IRoomTokenResolver
{
    public async Task<RoomResolution?> ResolveActiveTokenAsync(string token, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var qr = await db.RoomQrTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token && x.Status == RoomQrTokenStatus.Active, ct)
            .ConfigureAwait(false);
        if (qr is null)
        {
            return null;
        }

        var room = await db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == qr.RoomId, ct)
            .ConfigureAwait(false);
        if (room is null)
        {
            return null; // phòng đã xóa mềm (query filter loại) → không lộ.
        }

        return new RoomResolution(
            room.Id,
            room.ResortId,
            room.RoomNumber,
            room.Building,
            room.Floor,
            room.Status == RoomStatus.Active);
    }
}
