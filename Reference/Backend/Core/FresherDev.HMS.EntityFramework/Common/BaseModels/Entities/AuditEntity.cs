namespace FresherDev.HMS.EntityFramework;

public class AuditEntity<TKey> : Entity<TKey>, IAuditEntity<TKey>
    where TKey : struct
{
    public DateTimeOffset CreatedTime { get; set; }

    public DateTimeOffset? UpdatedTime { get; set; }

    public TKey? CreatedBy { get; set; }

    public TKey? UpdatedBy { get; set; }
}
