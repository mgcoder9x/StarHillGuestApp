namespace FresherDev.HMS.EntityFramework;

public class FullyEntity<TKey> : Entity<TKey>, IFullyEntity<TKey>
    where TKey : struct
{
    public bool IsEnabled { get; set; }

    public bool IsDeleted { get; set; }

    public TKey? CreatedBy { get; set; }

    public TKey? UpdatedBy { get; set; }

    public TKey? DeletedBy { get; set; }

    public DateTimeOffset? DeletedTime { get; set; }

    public DateTimeOffset CreatedTime { get; set; }

    public DateTimeOffset? UpdatedTime { get; set; }
}