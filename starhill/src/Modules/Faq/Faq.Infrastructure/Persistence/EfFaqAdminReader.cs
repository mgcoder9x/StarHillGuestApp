using Faq.Application;
using Microsoft.EntityFrameworkCore;

namespace Faq.Infrastructure.Persistence;

/// <summary>Editor read-model: full FAQ tree kể cả inactive, no-tracking, giữ ID/xmin/raw translations.</summary>
public sealed class EfFaqAdminReader(FaqDbContext db) : IFaqAdminReader
{
    public async Task<FaqAdminTreeSnapshot> LoadAsync(Guid resortId, CancellationToken ct = default)
    {
        var categories = await db.FaqCategories
            .AsNoTracking()
            .Where(category => category.ResortId == resortId)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = await db.FaqItems
            .AsNoTracking()
            .Where(item => item.ResortId == resortId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var categoryIds = categories.ConvertAll(category => category.Id);
        var itemIds = items.ConvertAll(item => item.Id);

        var categoryTranslations = await db.FaqCategoryTranslations
            .AsNoTracking()
            .Where(translation => categoryIds.Contains(translation.FaqCategoryId))
            .OrderBy(translation => translation.LanguageCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var itemTranslations = await db.FaqItemTranslations
            .AsNoTracking()
            .Where(translation => itemIds.Contains(translation.FaqItemId))
            .OrderBy(translation => translation.LanguageCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var categoryTranslationsById = categoryTranslations
            .GroupBy(translation => translation.FaqCategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var itemTranslationsById = itemTranslations
            .GroupBy(translation => translation.FaqItemId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var categorySnapshots = categories.ConvertAll(category => new FaqAdminCategorySnapshot(
            category.Id,
            category.RowVersion,
            category.Key,
            category.SortOrder,
            category.IsActive,
            categoryTranslationsById.TryGetValue(category.Id, out var values)
                ? values.ConvertAll(translation => new FaqAdminCategoryTranslationSnapshot(
                    translation.Id,
                    translation.RowVersion,
                    translation.LanguageCode,
                    translation.Name))
                : []));

        var itemSnapshots = items.ConvertAll(item => new FaqAdminItemSnapshot(
            item.Id,
            item.RowVersion,
            item.CategoryId,
            item.ParentId,
            item.SortOrder,
            item.IsActive,
            itemTranslationsById.TryGetValue(item.Id, out var values)
                ? values.ConvertAll(translation => new FaqAdminItemTranslationSnapshot(
                    translation.Id,
                    translation.RowVersion,
                    translation.LanguageCode,
                    translation.Question,
                    translation.AnswerHtmlSanitized))
                : []));

        return new FaqAdminTreeSnapshot(categorySnapshots, itemSnapshots);
    }
}
