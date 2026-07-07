using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FresherDev.HMS.EntityFramework;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly DbSet<TEntity> dbSet;
    private DbContext dbContext;

    public BaseRepository(ApplicationDbContext dbContext)
    {
        this.dbSet = dbContext.Set<TEntity>();
        this.dbContext = dbContext;
    }

    #region Raw

    public IQueryable<TEntity> AsQueryable()
    {
        return this.dbSet.AsQueryable();
    }

    public IQueryable<TEntity> FromSqlRaw(string sql, params object[] parameters)
    {
        return this.dbSet.FromSqlRaw(sql, parameters);
    }

    #endregion

    #region Find

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await this.dbSet.Where(filter).FirstOrDefaultAsync();
    }

    public async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        params Expression<Func<TEntity, object>>[] navigationPropertyPaths)
    {
        var query = this.dbSet.Where(filter);

        foreach (var item in navigationPropertyPaths)
        {
            query = query.Include(item);
        }

        return await query.FirstOrDefaultAsync();
    }

    #endregion

    #region Get

    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return await this.dbSet.WhereIf(filter).ToListAsync();
    }

    public async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] navigationPropertyPaths)
    {
        var query = this.dbSet.WhereIf(filter);
        foreach (var item in navigationPropertyPaths)
        {
            query = query.Include(item);
        }

        return await query.ToListAsync();
    }

    #endregion

    #region Count, Any

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return await this.dbSet.WhereIf(filter).CountAsync();
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return await this.dbSet.WhereIf(filter).AnyAsync();
    }

    #endregion

    #region Add

    public virtual async Task<TEntity?> AddAsync(TEntity entity)
    {
        var result = await this.dbSet.AddAsync(entity);
        await this.dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await dbSet.AddRangeAsync(entities);
        await dbContext.SaveChangesAsync();
    }

    #endregion

    #region Update

    public virtual async Task UpdateAsync(TEntity entity)
    {
        this.dbSet.Update(entity);
        await dbContext.SaveChangesAsync();
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
    {
        this.dbSet.UpdateRange(entities);
        await dbContext.SaveChangesAsync();
    }

    public virtual async Task ExecuteUpdateAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls)
    {
        this.dbSet.Where(predicate).ExecuteUpdate(setPropertyCalls);
        await dbContext.SaveChangesAsync();
    }

    #endregion

    #region Delete

    public virtual async Task<int> DeleteAsync(TEntity entity)
    {
        this.dbSet.Remove(entity);
        return await dbContext.SaveChangesAsync();
    }

    public virtual async Task<int> DeleteRangeAsync(params TEntity[] entities)
    {
        this.dbSet.RemoveRange(entities);
        return await dbContext.SaveChangesAsync();
    }

    public virtual async Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> expression)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            var updatedCount = await this.dbSet.Where(expression).ExecuteUpdateAsync(
                                x => x.SetProperty(p => ((ISoftDelete)p).IsDeleted, true)
                                    .SetProperty(p => ((ISoftDelete)p).DeletedTime, DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();
            return updatedCount;
        }

        var deleteCount = await this.dbSet.Where(expression).ExecuteDeleteAsync();
        await dbContext.SaveChangesAsync();
        return deleteCount;
    }

    #endregion
}