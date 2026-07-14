using Microsoft.EntityFrameworkCore;
using ResortConfig.Contracts.Queries;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IResortGuestConfigQuery"/> — read-only (AsNoTracking). Gom Resort + ResortSettings +
/// ResortLanguage (enabled, theo SortOrder) của một resort thành <see cref="ResortGuestConfig"/>. FAIL-CLOSED
/// (QR-AD-024): thiếu resort / thiếu settings / không có ngôn ngữ mặc định hợp lệ → <c>null</c>. Đăng ký scoped ở
/// <c>AddResortConfigInfrastructure</c> (dùng chung scope/DbContext).
/// </summary>
public sealed class EfResortGuestConfigQuery(ResortConfigDbContext db) : IResortGuestConfigQuery
{
    public async Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default)
    {
        var resort = await db.Resorts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resortId, ct)
            .ConfigureAwait(false);
        if (resort is null)
        {
            return null;
        }

        var settings = await db.ResortSettingsSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ResortId == resortId, ct)
            .ConfigureAwait(false);
        if (settings is null)
        {
            return null;
        }

        var languages = await db.ResortLanguages
            .AsNoTracking()
            .Where(l => l.ResortId == resortId && l.IsEnabled)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // FAIL-CLOSED: phải có đúng ngôn ngữ mặc định hợp lệ (enabled + IsDefault). DB đã enforce "đúng một default"
        // (ux_lang_default). Nếu default bị disable/thiếu → coi như cấu hình không hợp lệ, KHÔNG đoán mặc định.
        var defaultLanguage = languages.Find(l => l.IsDefault);
        if (defaultLanguage is null)
        {
            return null;
        }

        return new ResortGuestConfig(
            resort.Id,
            resort.Name,
            resort.LogoUrl,
            languages.ConvertAll(l => l.Code),
            defaultLanguage.Code,
            settings.FaqEnabled,
            settings.ChatEnabled,
            settings.HousekeepingEnabled,
            settings.RequireRuleAckForFaq,
            settings.RequireRuleAckForChat,
            settings.RequireRuleAckForHousekeeping,
            settings.PortalWindowMinutes,
            settings.VisitIdleExpiryHours);
    }
}
