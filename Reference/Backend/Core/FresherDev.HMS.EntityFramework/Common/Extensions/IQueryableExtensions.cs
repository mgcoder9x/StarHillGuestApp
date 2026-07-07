using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.EntityFramework;

public static class IQueryableExtensions
{
    public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> query, string propertyName)
        where TEntity : IEntity
    {
        var entityType = typeof(TEntity);

        // Create x=>x.PropName
        var propertyInfo = entityType.GetProperty(propertyName);

        if (propertyInfo == null)
            return query;

        ParameterExpression arg = Expression.Parameter(entityType, "x");
        MemberExpression property = Expression.Property(arg, propertyName);
        var selector = Expression.Lambda(property, new ParameterExpression[] { arg });

        // Get System.Linq.Queryable.OrderBy() method.
        var enumarableType = typeof(System.Linq.Queryable);
        var method = enumarableType.GetMethods()
             .Where(m => m.Name == "OrderBy" && m.IsGenericMethodDefinition)
             .Where(m =>
             {
                 var parameters = m.GetParameters().ToList();
                 //Put more restriction here to ensure selecting the right overload                
                 return parameters.Count == 2;//overload that has 2 parameters
             }).Single();

        // The linq's OrderBy<TSource, TKey> has two generic types, which provided here
        MethodInfo genericMethod = method.MakeGenericMethod(entityType, propertyInfo.PropertyType);

        /* Call query.OrderBy(selector), with query and selector: x=> x.PropName
          Note that we pass the selector as Expression to the method and we don't compile it.
          By doing so EF can extract "order by" columns and generate SQL for it.*/
        var newQuery = genericMethod.Invoke(genericMethod, new object[] { query, selector }) as IOrderedQueryable<TEntity>;

        return newQuery ?? query;
    }

    public static IQueryable<TEntity> OrderByDescending<TEntity>(this IQueryable<TEntity> query, string propertyName)
        where TEntity : IEntity
    {
        var entityType = typeof(TEntity);

        // Create x=>x.PropName
        var propertyInfo = entityType.GetProperty(propertyName);

        if (propertyInfo == null)
            return query;

        ParameterExpression arg = Expression.Parameter(entityType, "x");
        MemberExpression property = Expression.Property(arg, propertyName);
        var selector = Expression.Lambda(property, new ParameterExpression[] { arg });

        // Get System.Linq.Queryable.OrderBy() method.
        var enumarableType = typeof(System.Linq.Queryable);
        var method = enumarableType.GetMethods()
             .Where(m => m.Name == "OrderByDescending" && m.IsGenericMethodDefinition)
             .Where(m =>
             {
                 var parameters = m.GetParameters().ToList();
                 //Put more restriction here to ensure selecting the right overload                
                 return parameters.Count == 2;//overload that has 2 parameters
             }).Single();

        // The linq's OrderBy<TSource, TKey> has two generic types, which provided here
        MethodInfo genericMethod = method.MakeGenericMethod(entityType, propertyInfo.PropertyType);

        /* Call query.OrderBy(selector), with query and selector: x=> x.PropName
          Note that we pass the selector as Expression to the method and we don't compile it.
          By doing so EF can extract "order by" columns and generate SQL for it.*/
        var newQuery = genericMethod.Invoke(genericMethod, new object[] { query, selector }) as IOrderedQueryable<TEntity>;

        return newQuery ?? query;
    }

    public static IQueryable<TEntity> WhereIf<TEntity>(this IQueryable<TEntity> query, bool condition, Expression<Func<TEntity, bool>> predicate)
    {
        if (!condition)
        {
            return query;
        }

        return query.Where(predicate);
    }

    public static IQueryable<TEntity> WhereIf<TEntity>(this IQueryable<TEntity> query, Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return query;
        }

        return query.Where(predicate);
    }

    public static IQueryable<TEntity> ApplyCriteria<TEntity, TCriteriaBuilder, TCriteria>(this IQueryable<TEntity> query, TCriteria criteria)
        where TEntity : IEntity
        where TCriteria : ICriteria
        where TCriteriaBuilder : CriteriaBuilder<TEntity, TCriteria>
    {
        var instance = Activator.CreateInstance(typeof(TCriteriaBuilder), criteria);
        if (instance == null)
        {
            return query;
        }

        return query.Where(((TCriteriaBuilder)instance).Build());
    }

    public static async Task<IPaginationOutput<TEntity>> ToPaginationAsync<TEntity>(this IQueryable<TEntity> query, IPaginationInput input)
        where TEntity : class, IEntity
    {
        var total = await query.CountAsync();

        if (!string.IsNullOrWhiteSpace(input.OrderColumn))
        {
            query = input.Direction == OrderDirection.Ascending
                ? query.OrderBy(input.OrderColumn)
                : query.OrderByDescending(input.OrderColumn);
        }

        var items = await query
                    .Skip((input.Page - 1) * input.PageSize)
                    .Take(input.PageSize)
                    .ToListAsync();

        return new PaginationOutput<TEntity>()
        {
            Page = input.Page,
            PageSize = input.PageSize,
            Total = total,
            Items = items,
        };
    }
}