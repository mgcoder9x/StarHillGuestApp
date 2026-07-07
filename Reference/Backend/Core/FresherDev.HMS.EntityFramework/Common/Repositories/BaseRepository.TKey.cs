using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.EntityFramework;

public class BaseRepository<TEntity, TKey> : BaseRepository<TEntity>, IBaseRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : struct
{
    private readonly DbSet<TEntity> dbSet;
    private readonly DbContext dbContext;

    public BaseRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        this.dbSet = dbContext.Set<TEntity>();
        this.dbContext = dbContext;
    }

    public virtual async Task<TEntity?> FindAsync(TKey id)
    {
        return await this.dbSet.FindAsync(id);
    }

    public virtual async Task<bool> ExistAsync(TKey id)
    {
        return await this.dbSet.AnyAsync(x => x.Id!.Equals(id));
    }

    public virtual async Task<int> DeleteAsync(TKey id)
    {
        var count = await dbSet.Where(x => x.Id!.Equals(id)).ExecuteDeleteAsync();
        await this.dbContext.SaveChangesAsync();
        return count;
    }
}
