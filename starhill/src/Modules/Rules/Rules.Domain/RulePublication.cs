using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// SNAPSHOT BẤT BIẾN mỗi lần Publish — KHÁCH ĐỌC TỪ ĐÂY (CP4). Đúng MỘT <see cref="IsCurrent"/>=true mỗi resort
/// (partial unique <c>ux_rule_publication_current</c>). KHÔNG concurrency token (bất biến sau publish — không sửa).
/// <see cref="Version"/> tăng dần do use case Publish (server), KHÔNG do DB sinh. ResortId/PublishedByUserId Guid trần.
/// </summary>
public sealed class RulePublication : Entity
{
    public required Guid ResortId { get; set; }

    public int Version { get; set; }

    public DateTimeOffset PublishedAt { get; set; }

    public Guid? PublishedByUserId { get; set; }

    public string? ChangeNote { get; set; }

    public bool IsCurrent { get; set; }
}
