using Microsoft.EntityFrameworkCore.Storage;

namespace FresherDev.HMS.EntityFramework;

public partial class UnitOfWork : IUnitOfWork, IDisposable
{
    private ApplicationDbContext dbContext;

    private readonly Dictionary<Type, object> implementRepositories;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
        this.implementRepositories = new Dictionary<Type, object>();
    }

    public IDbContextTransaction CreateTransaction()
    {
        return dbContext.Database.BeginTransaction();
    }

    public TRepo GetRepository<TRepo>()
        where TRepo : class, IBaseRepository
    {
        var type = typeof(TRepo);
        if (!this.implementRepositories.ContainsKey(type))
        {
            var instance = Activator.CreateInstance(type, this.dbContext);
            if (instance == null)
            {
                throw new Exception($"Could not found Repository: {nameof(TRepo)}");
            }

            this.implementRepositories[type] = instance;
        }

        return (TRepo)this.implementRepositories[type];
    }

    public IBaseRepository<TEntity> GetGenericRepository<TEntity>()
       where TEntity : class, IEntity
    {
        var type = typeof(TEntity);
        if (!this.implementRepositories.ContainsKey(type))
        {
            var instance = new BaseRepository<TEntity>(this.dbContext);
            this.implementRepositories[type] = instance;
        }

        return (IBaseRepository<TEntity>)this.implementRepositories[type];
    }

    public IBaseRepository<TEntity, TKey> GetGenericRepository<TEntity, TKey>()
       where TEntity : class, IEntity<TKey>
       where TKey : struct
    {
        var type = typeof(TEntity);
        if (!this.implementRepositories.ContainsKey(type))
        {
            var instance = new BaseRepository<TEntity, TKey>(this.dbContext);
            this.implementRepositories[type] = instance;
        }

        return (IBaseRepository<TEntity, TKey>)this.implementRepositories[type];
    }

    public void Dispose()
    {
        this.dbContext.Dispose();
    }
}