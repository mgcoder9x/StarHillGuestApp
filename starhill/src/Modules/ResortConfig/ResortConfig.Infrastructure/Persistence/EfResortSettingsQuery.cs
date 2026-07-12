using Microsoft.EntityFrameworkCore;
using ResortConfig.Contracts.Queries;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IResortSettingsQuery"/> — read-only (AsNoTracking), single-resort (một bản ghi
/// ResortSettings). Map entity Domain → <see cref="ResortSettingsSnapshot"/> (DTO Contracts) để KHÔNG lộ
/// entity ra ngoài module. Đăng ký scoped ở <c>AddResortConfigInfrastructure</c> (dùng chung scope/DbContext).
/// </summary>
public sealed class EfResortSettingsQuery(ResortConfigDbContext db) : IResortSettingsQuery
{
    public async Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default)
    {
        var s = await db.ResortSettingsSet
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return s is null
            ? null
            : new ResortSettingsSnapshot(
                s.ResortId,
                s.FaqEnabled,
                s.ChatEnabled,
                s.HousekeepingEnabled,
                s.RequireRuleAckForFaq,
                s.RequireRuleAckForChat,
                s.RequireRuleAckForHousekeeping,
                s.PortalWindowMinutes,
                s.VisitIdleExpiryHours,
                s.GuestWebBaseUrl,
                s.MaxMessageLength,
                s.MessageRateLimitPerMinute,
                s.HousekeepingRateLimitPerHour);
    }
}
