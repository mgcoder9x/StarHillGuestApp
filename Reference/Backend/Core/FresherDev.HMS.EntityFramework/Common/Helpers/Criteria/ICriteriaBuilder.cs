using System.Linq.Expressions;

namespace FresherDev.HMS.EntityFramework;

public interface ICriteriaBuilder<TEntity>
    where TEntity : IEntity
{
    Expression<Func<TEntity, bool>> Build();
}
