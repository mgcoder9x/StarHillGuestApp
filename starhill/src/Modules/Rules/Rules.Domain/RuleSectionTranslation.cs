using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Bản dịch (theo ngôn ngữ) của một <see cref="RuleSection"/> Draft. <see cref="BodyHtmlSanitized"/> LƯU nội dung
/// ĐÃ sanitize (Req 8.6/CP12 — sanitize-on-save). Unique <c>(RuleSectionId, LanguageCode)</c>. Concurrency (CP15).
/// (D-Rules.4 sẽ cho implement <c>ITranslation</c> để dùng <c>ITranslationResolver</c> fallback — QR-N-030.)
/// </summary>
public sealed class RuleSectionTranslation : Entity, IHasConcurrencyToken
{
    public required Guid RuleSectionId { get; set; }

    public required string LanguageCode { get; set; }

    public string? Title { get; set; }

    public string? BodyHtmlSanitized { get; set; }

    public uint RowVersion { get; set; }
}
