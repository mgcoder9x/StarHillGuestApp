namespace FresherDev.HMS.EntityFramework;

public interface IAuditEntity
{
    DateTimeOffset CreatedTime { get; set; }

    DateTimeOffset? UpdatedTime { get; set; }
}

public interface IAuditEntity<TKey> : IAuditEntity
    where TKey : struct
{
    public TKey? CreatedBy { get; set; }

    public TKey? UpdatedBy { get; set; }
}
