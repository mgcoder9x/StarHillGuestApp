namespace Bedrock.Domain.Entities;

/// <summary>
/// Base entity kèm audit (thời điểm + actor tạo/sửa). Việc SET giá trị (từ IClock/ICurrentUser)
/// là trách nhiệm của Infrastructure (interceptor SaveChanges) — kernel chỉ khai contract dữ liệu.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
