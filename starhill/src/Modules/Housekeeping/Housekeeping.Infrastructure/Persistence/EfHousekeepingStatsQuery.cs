using Housekeeping.Contracts;
using Housekeeping.Domain;
using Microsoft.EntityFrameworkCore;

namespace Housekeeping.Infrastructure.Persistence;

/// <summary>
/// Impl EF <see cref="IHousekeepingStatsQuery"/> (no-tracking, đọc-đếm) cho Host Dashboard. Ticket MỞ =
/// Status ∈ {Requested, InProgress} (mirror partial-unique "1 ticket mở/phòng"), scope theo resort.
/// </summary>
public sealed class EfHousekeepingStatsQuery(HousekeepingDbContext db) : IHousekeepingStatsQuery
{
    public Task<int> CountOpenTicketsAsync(Guid resortId, CancellationToken ct = default) =>
        db.HousekeepingTickets
            .AsNoTracking()
            .CountAsync(
                t => t.ResortId == resortId
                     && (t.Status == HousekeepingStatus.Requested || t.Status == HousekeepingStatus.InProgress),
                ct);
}
