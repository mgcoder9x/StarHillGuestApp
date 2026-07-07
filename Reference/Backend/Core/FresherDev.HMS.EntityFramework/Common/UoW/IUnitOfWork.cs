using Microsoft.EntityFrameworkCore.Storage;

namespace FresherDev.HMS.EntityFramework;

public interface IUnitOfWork
{
    TRepo GetRepository<TRepo>()
        where TRepo : class, IBaseRepository;

    IBaseRepository<TEntity> GetGenericRepository<TEntity>()
       where TEntity : class, IEntity;

    IBaseRepository<TEntity, TKey> GetGenericRepository<TEntity, TKey>()
       where TEntity : class, IEntity<TKey>
       where TKey : struct;

    IDbContextTransaction CreateTransaction();
}