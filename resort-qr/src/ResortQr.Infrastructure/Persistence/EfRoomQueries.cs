using ResortQr.Application.Common;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Read-model phòng (EF, AsNoTracking). Nằm trong namespace Persistence → LOẠI khỏi Scrutor auto-scan
/// (coupled DbContext) → wire tường minh trong <c>AddResortQrDatabase</c>. Query filter tự loại soft-deleted.
/// ActiveTokenPreview qua correlated subquery (không lộ token đầy đủ — Req 11.6).
/// </summary>
public sealed class EfRoomQueries : IRoomQueries
{
    private readonly AppDbContext _db;

    public EfRoomQueries(AppDbContext db) => _db = db;

    public async Task<PagedResult<RoomListItem>> ListAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = _db.Rooms.AsNoTracking();
        var total = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderBy(r => r.RoomNumber)
            .ThenBy(r => r.Id)
            .Skip(request.Skip)
            .Take(request.SafePageSize)
            .Select(r => new RoomListItem(
                r.Id,
                r.RoomNumber,
                r.Building,
                r.Floor,
                r.Status,
                _db.RoomQrTokens
                    .Where(t => t.RoomId == r.Id && t.Status == RoomQrTokenStatus.Active)
                    .Select(t => t.TokenPreview)
                    .FirstOrDefault(),
                r.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<RoomListItem>(items, request.SafePage, request.SafePageSize, total);
    }

    public Task<RoomListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoomListItem(
                r.Id,
                r.RoomNumber,
                r.Building,
                r.Floor,
                r.Status,
                _db.RoomQrTokens
                    .Where(t => t.RoomId == r.Id && t.Status == RoomQrTokenStatus.Active)
                    .Select(t => t.TokenPreview)
                    .FirstOrDefault(),
                r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
