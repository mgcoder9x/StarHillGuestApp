namespace FresherDev.HMS.EntityFramework;

public interface IFullyEntity<TKey> : IAuditEntity<TKey>, ISoftDelete<TKey>, IEnableEntity
    where TKey : struct
{
}
