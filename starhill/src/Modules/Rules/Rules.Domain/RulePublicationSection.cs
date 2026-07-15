using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Bản sao ĐÔNG CỨNG của một section tại thời điểm Publish (thuộc <see cref="RulePublication"/>). Bất biến — không
/// concurrency token. Giữ nguyên cấu hình đọc để khách thấy đúng snapshot đã publish (CP4).
/// </summary>
public sealed class RulePublicationSection : Entity
{
    public required Guid RulePublicationId { get; set; }

    public required string Key { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public bool RequireScrollEnd { get; set; }

    public int MinReadSeconds { get; set; }
}
