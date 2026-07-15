using Bedrock.Domain.Entities;

namespace Faq.Domain;

/// <summary>
/// Danh mục FAQ của một resort (Req 4.1) — lễ tân/admin soạn. <see cref="Key"/> BẤT BIẾN sau tạo (khóa ổn định,
/// mirror <c>RuleSection.Key</c>). <see cref="ResortId"/> là Guid TRẦN (không FK chéo-schema resort_config —
/// QR-AD-002). <see cref="IsActive"/> ẩn/hiện với khách (guest read chỉ trả active — Req 4.4). FAQ sửa trực tiếp
/// (KHÔNG Draft→Publish như Rules — requirements không yêu cầu version/ack cho FAQ). Concurrency token (xmin) chống
/// hai người sửa đè âm thầm (CP15).
/// </summary>
public sealed class FaqCategory : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    /// <summary>Khóa ổn định của category (định danh/resort — unique <c>(ResortId, Key)</c>).</summary>
    public required string Key { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public uint RowVersion { get; set; }
}
