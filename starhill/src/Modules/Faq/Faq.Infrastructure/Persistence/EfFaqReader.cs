using Faq.Application;
using Microsoft.EntityFrameworkCore;

namespace Faq.Infrastructure.Persistence;

/// <summary>
/// Impl EF read-model <see cref="IFaqReader"/> — đọc cây FAQ ĐANG ACTIVE của resort (no-tracking, guest read).
/// CHỈ category + item <c>IsActive</c> (Req 4.4 — khách không thấy nội dung tắt); item có parent inactive sẽ tự
/// biến mất khỏi cây (parent không nằm trong tập active để đệ quy — hành vi mong muốn: tắt cha ẩn cả nhánh). 4 truy
/// vấn phẳng (category → category-translation → item → item-translation theo tập id) rồi ghép trong bộ nhớ (F9 —
/// không Include lồng; use case dựng cha-con). Mirror EfRulePublicationReader.
/// </summary>
public sealed class EfFaqReader(FaqDbContext db) : IFaqReader
{
    public async Task<FaqTreeSnapshot> LoadActiveTreeAsync(Guid resortId, CancellationToken ct = default)
    {
        var categories = await db.FaqCategories
            .AsNoTracking()
            .Where(c => c.ResortId == resortId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = await db.FaqItems
            .AsNoTracking()
            .Where(i => i.ResortId == resortId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var categoryIds = categories.ConvertAll(c => c.Id);
        var itemIds = items.ConvertAll(i => i.Id);

        var categoryTranslations = await db.FaqCategoryTranslations
            .AsNoTracking()
            .Where(t => categoryIds.Contains(t.FaqCategoryId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var itemTranslations = await db.FaqItemTranslations
            .AsNoTracking()
            .Where(t => itemIds.Contains(t.FaqItemId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var categoryTranslationsById = categoryTranslations
            .GroupBy(t => t.FaqCategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var itemTranslationsById = itemTranslations
            .GroupBy(t => t.FaqItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var categorySnapshots = categories.ConvertAll(c => new FaqCategorySnapshot(
            c.Id,
            c.Key,
            c.SortOrder,
            categoryTranslationsById.TryGetValue(c.Id, out var ctr)
                ? ctr.Select(t => new FaqCategoryTranslationSnapshot(t.LanguageCode, t.Name)).ToList()
                : []));

        var itemSnapshots = items.ConvertAll(i => new FaqItemSnapshot(
            i.Id,
            i.CategoryId,
            i.ParentId,
            i.SortOrder,
            itemTranslationsById.TryGetValue(i.Id, out var itr)
                ? itr.Select(t => new FaqItemTranslationSnapshot(t.LanguageCode, t.Question, t.AnswerHtmlSanitized)).ToList()
                : []));

        return new FaqTreeSnapshot(categorySnapshots, itemSnapshots);
    }
}
