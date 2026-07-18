using Microsoft.EntityFrameworkCore;
using Rules.Application;

namespace Rules.Infrastructure.Persistence;

/// <summary>Editor read-model: full Draft, no-tracking, giữ ID + xmin + mọi translation.</summary>
public sealed class EfRuleAdminReader(RulesDbContext db) : IRuleAdminReader
{
    public async Task<RuleAdminDraftSnapshot?> LoadAsync(Guid resortId, CancellationToken ct = default)
    {
        var ruleSet = await db.RuleSets
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.ResortId == resortId, ct)
            .ConfigureAwait(false);
        if (ruleSet is null)
        {
            return null;
        }

        var sections = await db.RuleSections
            .AsNoTracking()
            .Where(section => section.RuleSetId == ruleSet.Id)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sectionIds = sections.ConvertAll(section => section.Id);
        var translations = await db.RuleSectionTranslations
            .AsNoTracking()
            .Where(translation => sectionIds.Contains(translation.RuleSectionId))
            .OrderBy(translation => translation.LanguageCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var translationsBySection = translations
            .GroupBy(translation => translation.RuleSectionId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var snapshots = sections.ConvertAll(section => new RuleAdminSectionSnapshot(
            section.Id,
            section.RowVersion,
            section.Key,
            section.SortOrder,
            section.IsRequired,
            section.RequireScrollEnd,
            section.MinReadSeconds,
            translationsBySection.TryGetValue(section.Id, out var values)
                ? values.ConvertAll(translation => new RuleAdminTranslationSnapshot(
                    translation.Id,
                    translation.RowVersion,
                    translation.LanguageCode,
                    translation.Title,
                    translation.BodyHtmlSanitized))
                : []));

        return new RuleAdminDraftSnapshot(ruleSet.Id, ruleSet.RowVersion, snapshots);
    }
}
