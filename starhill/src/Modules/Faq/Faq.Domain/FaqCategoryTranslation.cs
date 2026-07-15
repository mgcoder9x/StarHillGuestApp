using Bedrock.Domain.Entities;

namespace Faq.Domain;

/// <summary>
/// Bản dịch (theo ngôn ngữ) tên của một <see cref="FaqCategory"/>. Unique <c>(FaqCategoryId, LanguageCode)</c>
/// (CP5 nguồn). <see cref="Name"/> nullable để mô hình "có row nhưng rỗng → fallback" (E-Faq.4 dùng
/// <c>ITranslationResolver</c>). Concurrency token (CP15). <see cref="Name"/> sanitize-on-save (text thuần nhưng
/// vẫn qua sanitizer cho nhất quán — E-Faq.2).
/// </summary>
public sealed class FaqCategoryTranslation : Entity, IHasConcurrencyToken
{
    public required Guid FaqCategoryId { get; set; }

    public required string LanguageCode { get; set; }

    public string? Name { get; set; }

    public uint RowVersion { get; set; }
}
