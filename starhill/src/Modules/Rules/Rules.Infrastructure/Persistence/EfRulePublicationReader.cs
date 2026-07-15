using Microsoft.EntityFrameworkCore;
using Rules.Application;

namespace Rules.Infrastructure.Persistence;

/// <summary>
/// Impl EF read-model <see cref="IRulePublicationReader"/> — đọc bản nội quy HIỆN HÀNH (publication <c>IsCurrent</c>)
/// của resort (no-tracking, chỉ đọc cho guest). 3 truy vấn: publication IsCurrent → section (ordered) → translation
/// theo tập sectionId; ghép trong bộ nhớ (giữ thứ tự SortOrder tường minh). Trả <c>null</c> nếu resort chưa publish.
/// </summary>
public sealed class EfRulePublicationReader(RulesDbContext db) : IRulePublicationReader
{
    public async Task<CurrentRulesSnapshot?> LoadCurrentAsync(Guid resortId, CancellationToken ct = default)
    {
        var publication = await db.RulePublications
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ResortId == resortId && p.IsCurrent, ct)
            .ConfigureAwait(false);
        if (publication is null)
        {
            return null;
        }

        var sections = await db.RulePublicationSections
            .AsNoTracking()
            .Where(s => s.RulePublicationId == publication.Id)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sectionIds = sections.ConvertAll(s => s.Id);

        var translations = await db.RulePublicationSectionTranslations
            .AsNoTracking()
            .Where(t => sectionIds.Contains(t.RulePublicationSectionId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var translationsBySection = translations
            .GroupBy(t => t.RulePublicationSectionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sectionSnapshots = new List<PublishedSectionSnapshot>(sections.Count);
        foreach (var section in sections)
        {
            var sectionTranslations = translationsBySection.TryGetValue(section.Id, out var list)
                ? list.Select(t => new PublishedTranslationSnapshot(t.LanguageCode, t.Title, t.BodyHtmlSanitized)).ToList()
                : [];

            sectionSnapshots.Add(new PublishedSectionSnapshot(
                section.Key,
                section.SortOrder,
                section.IsRequired,
                section.RequireScrollEnd,
                section.MinReadSeconds,
                sectionTranslations));
        }

        return new CurrentRulesSnapshot(publication.Id, publication.Version, sectionSnapshots);
    }
}
