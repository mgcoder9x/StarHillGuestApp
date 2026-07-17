using Microsoft.EntityFrameworkCore;
using Rules.Contracts;
using Rules.Domain;

namespace Rules.Infrastructure.Persistence;

/// <summary>
/// Impl EF <see cref="IRulesStatsQuery"/> (no-tracking, đọc-đếm) cho Host Dashboard. Đếm lượt xác nhận nội quy
/// (<see cref="RuleAcknowledgement"/>) có AcceptedAt ≥ <c>since</c> (Host truyền = đầu ngày), scope theo resort.
/// </summary>
public sealed class EfRulesStatsQuery(RulesDbContext db) : IRulesStatsQuery
{
    public Task<int> CountAcknowledgementsSinceAsync(Guid resortId, DateTimeOffset since, CancellationToken ct = default) =>
        db.RuleAcknowledgements
            .AsNoTracking()
            .CountAsync(a => a.ResortId == resortId && a.AcceptedAt >= since, ct);
}
