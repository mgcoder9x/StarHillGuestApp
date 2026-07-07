using ResortQr.Application.Common;
using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Read-model phòng (CQRS-lite, 02 §6.1): KHÔNG xuyên IQueryable ra ngoài. Impl ở Infrastructure (coupled
/// DbContext) — wire tường minh (loại khỏi Scrutor auto-scan). Loại soft-deleted qua query filter.
/// </summary>
public interface IRoomQueries : IScopedService
{
    Task<PagedResult<RoomListItem>> ListAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<RoomListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
