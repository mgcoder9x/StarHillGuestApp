using ResortConfig.Contracts.Localization;

namespace Faq.Application;

/// <summary>Read-model riêng cho editor FAQ: kể cả inactive, giữ ID/xmin và mọi translation per-language.</summary>
public interface IFaqAdminReader
{
    Task<FaqAdminTreeSnapshot> LoadAsync(Guid resortId, CancellationToken ct = default);
}

public sealed record FaqAdminTreeSnapshot(
    IReadOnlyList<FaqAdminCategorySnapshot> Categories,
    IReadOnlyList<FaqAdminItemSnapshot> Items);

public sealed record FaqAdminCategorySnapshot(
    Guid CategoryId,
    uint RowVersion,
    string Key,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<FaqAdminCategoryTranslationSnapshot> Translations);

public sealed record FaqAdminCategoryTranslationSnapshot(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Name) : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Name);
}

public sealed record FaqAdminItemSnapshot(
    Guid ItemId,
    uint RowVersion,
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<FaqAdminItemTranslationSnapshot> Translations);

public sealed record FaqAdminItemTranslationSnapshot(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Question,
    string? AnswerHtmlSanitized) : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Question) || !string.IsNullOrWhiteSpace(AnswerHtmlSanitized);
}
