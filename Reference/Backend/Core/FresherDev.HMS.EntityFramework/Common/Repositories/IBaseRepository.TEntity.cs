using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace FresherDev.HMS.EntityFramework;

public interface IBaseRepository<TEntity> : IBaseRepository
    where TEntity : class, IEntity
{
    #region Raw

    IQueryable<TEntity> AsQueryable();

    IQueryable<TEntity> FromSqlRaw(string sql, params object[] parameters);

    #endregion

    #region Find

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter);

    Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> filter,
        params Expression<Func<TEntity, object>>[] navigationPropertyPaths);

    #endregion

    #region Get

    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? filter = null);

    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] navigationPropertyPaths);

    #endregion

    #region Count, Any

    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null);

    #endregion

    #region Add 

    Task<TEntity?> AddAsync(TEntity entity);

    Task AddRangeAsync(IEnumerable<TEntity> entities);

    #endregion

    #region Update

    Task UpdateAsync(TEntity entity);

    Task UpdateRangeAsync(IEnumerable<TEntity> entities);

    Task ExecuteUpdateAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls);

    #endregion

    #region Delete

    Task<int> DeleteAsync(TEntity entitie);

    Task<int> DeleteRangeAsync(params TEntity[] entities);

    Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> expression);

    #endregion
}
