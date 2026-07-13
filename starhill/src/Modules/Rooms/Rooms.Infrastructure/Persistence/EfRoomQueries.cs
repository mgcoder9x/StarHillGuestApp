using Bedrock.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Rooms.Application;
using Rooms.Domain;

namespace Rooms.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IRoomQueries"/> (read-only, admin B-Rooms.4). Phòng xóa mềm bị global query filter
/// (ISoftDeletable) loại. Token Active preview/version lấy bằng truy vấn PHỤ đơn giản (page rooms trước → load
/// token Active theo <c>ids.Contains</c> → map in-memory) thay vì correlated-subquery lồng — tránh rủi ro dịch LINQ
/// phức tạp trên nhiều provider (D-D); ux_qr_active ⇒ ≤1 token Active/phòng nên map dictionary an toàn. Đăng ký
/// scoped ở <c>AddRoomsInfrastructure</c> (inject <see cref="RoomsDbContext"/> cụ thể, không cần keyed).
/// </summary>
public sealed class EfRoomQueries(RoomsDbContext db) : IRoomQueries
{
    public async Task<PagedResult<RoomListItem>> ListAsync(
        RoomStatus? status,
        PagedRequest paging,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paging);

        var query = db.Rooms.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var total = await query.LongCountAsync(ct).ConfigureAwait(false);

        var pageRooms = await query
            .OrderBy(r => r.Building)
            .ThenBy(r => r.RoomNumber)
            .Skip(paging.Skip)
            .Take(paging.SafePageSize)
            .Select(r => new RoomRow(r.Id, r.RoomNumber, r.Building, r.Floor, r.Status, r.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var actives = await LoadActiveTokensAsync(pageRooms.Select(r => r.Id), ct).ConfigureAwait(false);

        var items = pageRooms
            .Select(r => ToItem(r, actives))
            .ToList();

        return new PagedResult<RoomListItem>(items, paging.SafePage, paging.SafePageSize, total);
    }

    public async Task<RoomListItem?> GetByIdAsync(Guid roomId, CancellationToken ct = default)
    {
        var row = await db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new RoomRow(r.Id, r.RoomNumber, r.Building, r.Floor, r.Status, r.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        var actives = await LoadActiveTokensAsync([row.Id], ct).ConfigureAwait(false);
        return ToItem(row, actives);
    }

    private async Task<Dictionary<Guid, ActiveToken>> LoadActiveTokensAsync(
        IEnumerable<Guid> roomIds,
        CancellationToken ct)
    {
        var ids = roomIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await db.RoomQrTokens
            .AsNoTracking()
            .Where(t => t.Status == RoomQrTokenStatus.Active && ids.Contains(t.RoomId))
            .Select(t => new ActiveToken(t.RoomId, t.TokenPreview, t.Version))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // ux_qr_active ⇒ ≤1 Active/phòng → key duy nhất.
        return rows.ToDictionary(t => t.RoomId);
    }

    private static RoomListItem ToItem(RoomRow r, Dictionary<Guid, ActiveToken> actives)
    {
        var preview = actives.TryGetValue(r.Id, out var t) ? t.TokenPreview : null;
        var version = actives.TryGetValue(r.Id, out var t2) ? t2.Version : 0;
        return new RoomListItem(r.Id, r.RoomNumber, r.Building, r.Floor, r.Status, preview, version, r.CreatedAt);
    }

    private sealed record RoomRow(Guid Id, string RoomNumber, string? Building, int? Floor, RoomStatus Status, DateTimeOffset CreatedAt);

    private sealed record ActiveToken(Guid RoomId, string TokenPreview, int Version);
}
