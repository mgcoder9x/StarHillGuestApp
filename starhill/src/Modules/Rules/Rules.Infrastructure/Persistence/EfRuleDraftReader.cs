using Microsoft.EntityFrameworkCore;
using Rules.Application;

namespace Rules.Infrastructure.Persistence;

/// <summary>
/// Impl EF read-model <see cref="IRuleDraftReader"/> — đọc trọn Draft của resort (no-tracking, chỉ để COPY sang
/// publication). Inject <see cref="RulesDbContext"/> cụ thể (scoped, type riêng module nên đăng ký unkeyed an toàn;
/// ports generic mới cần keyed). Load 3 truy vấn (set → sections ordered → translations theo tập sectionId) rồi ghép
/// trong bộ nhớ (tránh Include lồng + giữ thứ tự SortOrder tường minh). Trả <c>null</c> nếu resort chưa có RuleSet.
/// </summary>
public sealed class EfRuleDraftReader(RulesDbContext db) : IRuleDraftReader
{
    public async Task<RuleDraftSnapshot?> LoadDraftAsync(Guid resortId, CancellationToken ct = default)
    {
        var ruleSet = await db.RuleSets
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.ResortId == resortId, ct)
            .ConfigureAwait(false);
        if (ruleSet is null)
        {
            return null;
        }

        var sections = await db.RuleSections
            .AsNoTracking()
            .Where(s => s.RuleSetId == ruleSet.Id)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sectionIds = sections.ConvertAll(s => s.Id);

        var translations = await db.RuleSectionTranslations
            .AsNoTracking()
            .Where(t => sectionIds.Contains(t.RuleSectionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var translationsBySection = translations
            .GroupBy(t => t.RuleSectionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sectionSnapshots = new List<RuleDraftSectionSnapshot>(sections.Count);
        foreach (var section in sections)
        {
            var sectionTranslations = translationsBySection.TryGetValue(section.Id, out var list)
                ? list.Select(t => new RuleDraftTranslationSnapshot(t.LanguageCode, t.Title, t.BodyHtmlSanitized)).ToList()
                : [];

            sectionSnapshots.Add(new RuleDraftSectionSnapshot(
                section.Key,
                section.SortOrder,
                section.IsRequired,
                section.RequireScrollEnd,
                section.MinReadSeconds,
                sectionTranslations));
        }

        return new RuleDraftSnapshot(ruleSet.Id, sectionSnapshots);
    }
}
