using ResortConfig.Contracts.Localization;

namespace Rules.Application;

/// <summary>
/// Read-model riêng cho editor Draft. Khác <see cref="IRuleDraftReader"/> dùng để publish/preview, port này giữ ID,
/// raw translation per-language và xmin <c>RowVersion</c> để client round-trip optimistic concurrency.
/// </summary>
public interface IRuleAdminReader
{
    Task<RuleAdminDraftSnapshot?> LoadAsync(Guid resortId, CancellationToken ct = default);
}

public sealed record RuleAdminDraftSnapshot(
    Guid RuleSetId,
    uint RowVersion,
    IReadOnlyList<RuleAdminSectionSnapshot> Sections);

public sealed record RuleAdminSectionSnapshot(
    Guid SectionId,
    uint RowVersion,
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    IReadOnlyList<RuleAdminTranslationSnapshot> Translations);

public sealed record RuleAdminTranslationSnapshot(
    Guid TranslationId,
    uint RowVersion,
    string LanguageCode,
    string? Title,
    string? BodyHtmlSanitized) : ITranslation
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(BodyHtmlSanitized);
}
