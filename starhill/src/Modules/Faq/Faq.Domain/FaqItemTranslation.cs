using Bedrock.Domain.Entities;

namespace Faq.Domain;

/// <summary>
/// Bản dịch (theo ngôn ngữ) của một <see cref="FaqItem"/>. <see cref="AnswerHtmlSanitized"/> LƯU nội dung ĐÃ
/// sanitize (Req 8.6/CP12 — sanitize-on-save); <see cref="Question"/> cũng sanitize (chống XSS mọi bề mặt). Unique
/// <c>(FaqItemId, LanguageCode)</c>. Nullable để mô hình "row rỗng → fallback" (E-Faq.4 <c>ITranslationResolver</c>).
/// Concurrency token (CP15).
/// </summary>
public sealed class FaqItemTranslation : Entity, IHasConcurrencyToken
{
    public required Guid FaqItemId { get; set; }

    public required string LanguageCode { get; set; }

    public string? Question { get; set; }

    public string? AnswerHtmlSanitized { get; set; }

    public uint RowVersion { get; set; }
}
