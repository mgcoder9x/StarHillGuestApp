using ResortConfig.Contracts.Localization;

namespace Rules.Application;

/// <summary>
/// Read-model NỘI-MODULE (CQRS-lite, F9 — không IQueryable) để guest đọc bản nội quy HIỆN HÀNH: <c>RulePublication</c>
/// <c>IsCurrent</c> của resort + section (theo SortOrder) + bản dịch mỗi section. Snapshot bất biến (no-tracking) —
/// khách CHỈ đọc từ đây, KHÔNG từ Draft (CP4). Trả <c>null</c> nếu resort chưa publish lần nào.
/// </summary>
public interface IRulePublicationReader
{
    Task<CurrentRulesSnapshot?> LoadCurrentAsync(Guid resortId, CancellationToken ct = default);
}

/// <summary>Snapshot bản nội quy hiện hành (publication IsCurrent) — chỉ đọc.</summary>
public sealed record CurrentRulesSnapshot(
    Guid PublicationId,
    int Version,
    IReadOnlyList<PublishedSectionSnapshot> Sections);

/// <summary>Section đã publish (đông cứng) + các bản dịch của nó.</summary>
public sealed record PublishedSectionSnapshot(
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    IReadOnlyList<PublishedTranslationSnapshot> Translations);

/// <summary>
/// Bản dịch đã publish — hiện thực <see cref="ITranslation"/> để <c>ITranslationResolver</c> fallback (CP5). Đặt
/// <see cref="ITranslation"/> trên DTO read-model (tầng Application, đã ref ResortConfig.Contracts) THAY VÌ entity
/// Domain (QR-DV-007) → giữ <c>Rules.Domain</c> thuần (không phụ thuộc Contracts module khác); i18n-resolution là
/// concern rendering/Application, không phải bất biến Domain. <see cref="HasContent"/> = Title HOẶC Body sau trim
/// khác rỗng ("có row nhưng rỗng" coi như thiếu → fallback).
/// </summary>
public sealed record PublishedTranslationSnapshot(string LanguageCode, string? Title, string? BodyHtmlSanitized)
    : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(BodyHtmlSanitized);
}
