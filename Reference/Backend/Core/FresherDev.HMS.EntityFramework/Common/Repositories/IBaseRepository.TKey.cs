namespace FresherDev.HMS.EntityFramework;

public interface IBaseRepository<TEntity, TKey> : IBaseRepository<TEntity>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
    Task<TEntity?> FindAsync(TKey id);

    Task<bool> ExistAsync(TKey id);

    Task<int> DeleteAsync(TKey id);
}