namespace Foundation.SharedKernel.Entities;

/// <summary>
/// Base entity kèm audit + concurrency token (xmin). Kế thừa khi entity cần vết tạo/sửa + chống ghi đè.
/// </summary>
public abstract class AuditableEntity : Entity, IAuditable, IConcurrencyAware
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>Map tới <c>xmin</c> (Npgsql <c>UseXminAsConcurrencyToken</c>).</summary>
    public uint RowVersion { get; set; }
}
