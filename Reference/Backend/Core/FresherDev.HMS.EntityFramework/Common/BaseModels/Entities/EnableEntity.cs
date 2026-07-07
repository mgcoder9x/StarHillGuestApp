namespace FresherDev.HMS.EntityFramework;

public class EnableEntity<TKey>: Entity<TKey>, IEnableEntity
    where TKey : struct
{
    public bool IsEnabled { get; set; }
}