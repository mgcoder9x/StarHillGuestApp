using System.Linq.Expressions;

namespace FresherDev.HMS.EntityFramework;

public abstract class CriteriaBuilder<TEntity, TCriteria> : ICriteriaBuilder<TEntity>
    where TEntity : IEntity
    where TCriteria : ICriteria
{
    private Expression<Func<TEntity, bool>> expression;

    public CriteriaBuilder(TCriteria criteria)
    {
        this.expression = PredicateBuilder.False<TEntity>();
        this.CreateModeling(criteria);
    }

    public abstract void CreateModeling(TCriteria criteria);

    public Expression<Func<TEntity, bool>> Build()
    {
        return this.expression;
    }

    protected void Or(Expression<Func<TEntity, bool>> other)
    {
        this.expression = this.expression.Or(other);
    }

    protected void And(Expression<Func<TEntity, bool>> expression2)
    {
        this.expression = this.expression.Or(expression2);
    }
}