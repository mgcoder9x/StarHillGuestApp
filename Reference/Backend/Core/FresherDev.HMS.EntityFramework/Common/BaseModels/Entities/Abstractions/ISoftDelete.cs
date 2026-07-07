namespace FresherDev.HMS.EntityFramework;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedTime { get; set; }    
}

public interface ISoftDelete<TKey> : ISoftDelete
    where TKey : struct
{
    public TKey? DeletedBy { get; set; }
}