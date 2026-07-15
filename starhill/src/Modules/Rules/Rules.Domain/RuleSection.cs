using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Section nội quy trong bản Draft (<see cref="RuleSet"/>) — admin sửa. Cấu hình đọc: <see cref="IsRequired"/>,
/// <see cref="RequireScrollEnd"/>, <see cref="MinReadSeconds"/>, <see cref="SortOrder"/> (Req 3.3/3.4/8.2). Đây
/// KHÔNG phải nguồn khách đọc — khi Publish sẽ đông cứng thành <see cref="RulePublicationSection"/>. Concurrency (CP15).
/// </summary>
public sealed class RuleSection : Entity, IHasConcurrencyToken
{
    public required Guid RuleSetId { get; set; }

    /// <summary>Khóa ổn định của section (để giữ tiến độ đọc khi đổi ngôn ngữ — Req 3, product design).</summary>
    public required string Key { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; }

    public bool RequireScrollEnd { get; set; }

    public int MinReadSeconds { get; set; }

    public uint RowVersion { get; set; }
}
