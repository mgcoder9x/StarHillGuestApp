namespace FresherDev.HMS.EntityFramework;

public class SoftDeleteEntity<TKey> : Entity<TKey>, ISoftDelete<TKey>
    where TKey : struct
{
    public bool IsDeleted { get; set; }

    public TKey? DeletedBy { get; set; }

    public DateTimeOffset? DeletedTime { get; set; }
}