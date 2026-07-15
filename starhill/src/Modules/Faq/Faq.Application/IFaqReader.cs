using ResortConfig.Contracts.Localization;

namespace Faq.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) để guest đọc CÂY FAQ ĐANG ACTIVE của resort: category
/// (active, theo SortOrder) + item (active, theo SortOrder) + bản dịch mỗi node. Trả snapshot phẳng (category list +
/// item list) — use case dựng cây cha-con trong bộ nhớ (giữ thứ tự tường minh, tránh Include lồng). Chỉ đọc
/// (no-tracking). Mirror <c>IRuleDraftReader</c>/<c>IRulePublicationReader</c>.
/// </summary>
public interface IFaqReader
{
    /// <summary>Cây FAQ active của resort (category + item + translations). Rỗng nếu resort chưa có FAQ active.</summary>
    Task<FaqTreeSnapshot> LoadActiveTreeAsync(Guid resortId, CancellationToken ct = default);
}

/// <summary>Snapshot phẳng cây FAQ active — category list + item list (use case ghép cha-con theo ParentId).</summary>
public sealed record FaqTreeSnapshot(
    IReadOnlyList<FaqCategorySnapshot> Categories,
    IReadOnlyList<FaqItemSnapshot> Items);

/// <summary>Category active + bản dịch (đã sắp theo SortOrder ở danh sách cha).</summary>
public sealed record FaqCategorySnapshot(
    Guid Id,
    string Key,
    int SortOrder,
    IReadOnlyList<FaqCategoryTranslationSnapshot> Translations);

/// <summary>
/// Bản dịch tên category — hiện thực <see cref="ITranslation"/> để <c>ITranslationResolver</c> fallback (CP5). Đặt
/// <see cref="ITranslation"/> trên DTO read-model (Application, đã ref ResortConfig.Contracts) THAY VÌ entity Domain
/// (QR-DV-007 — i18n là concern rendering/Application). <see cref="HasContent"/> = Name sau trim khác rỗng.
/// </summary>
public sealed record FaqCategoryTranslationSnapshot(string LanguageCode, string? Name) : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Name);
}

/// <summary>Item active + bản dịch. <see cref="ParentId"/> để use case dựng flow cha-con.</summary>
public sealed record FaqItemSnapshot(
    Guid Id,
    Guid CategoryId,
    Guid? ParentId,
    int SortOrder,
    IReadOnlyList<FaqItemTranslationSnapshot> Translations);

/// <summary>Bản dịch item — hiện thực <see cref="ITranslation"/>. <see cref="HasContent"/> = Question HOẶC Answer khác rỗng.</summary>
public sealed record FaqItemTranslationSnapshot(string LanguageCode, string? Question, string? AnswerHtmlSanitized) : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Question) || !string.IsNullOrWhiteSpace(AnswerHtmlSanitized);
}
