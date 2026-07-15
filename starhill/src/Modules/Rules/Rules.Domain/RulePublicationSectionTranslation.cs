using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Bản sao ĐÔNG CỨNG nội dung theo ngôn ngữ của một <see cref="RulePublicationSection"/> (đã sanitize khi copy từ
/// Draft — CP12). Bất biến — không concurrency token. Unique <c>(RulePublicationSectionId, LanguageCode)</c>.
/// </summary>
public sealed class RulePublicationSectionTranslation : Entity
{
    public required Guid RulePublicationSectionId { get; set; }

    public required string LanguageCode { get; set; }

    public string? Title { get; set; }

    public string? BodyHtmlSanitized { get; set; }
}
