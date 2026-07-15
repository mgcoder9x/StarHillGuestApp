using Bedrock.Domain.Entities;

namespace Faq.Domain;

/// <summary>
/// Một mục FAQ (câu hỏi) thuộc một <see cref="FaqCategory"/> (Req 4.1). <see cref="ParentId"/> self-reference tạo
/// FLOW cha-con (câu hỏi dẫn tới câu hỏi con) — <c>null</c> = item gốc. Bất biến cây (E-Faq.2 enforce ở use case):
/// parent phải CÙNG <see cref="CategoryId"/>, không tự trỏ mình, không tạo chu trình (chống render loop guest).
/// <see cref="ResortId"/> Guid TRẦN denormalize (query theo resort không cần join category). <see cref="IsActive"/>
/// ẩn/hiện với khách. Concurrency token (CP15).
/// </summary>
public sealed class FaqItem : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    public required Guid CategoryId { get; set; }

    /// <summary>Cha trong flow (self-reference, cùng category). <c>null</c> = item gốc.</summary>
    public Guid? ParentId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public uint RowVersion { get; set; }
}
