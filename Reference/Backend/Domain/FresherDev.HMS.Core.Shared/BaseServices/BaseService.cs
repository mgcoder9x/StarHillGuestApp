using AutoMapper;
using FresherDev.HMS.EntityFramework;
using Microsoft.EntityFrameworkCore.Storage;

namespace FresherDev.HMS.Core.Shared;

public abstract class BaseService : IBaseService
{
    private readonly IUnitOfWork unitOfWork;
    protected readonly IMapper Mapper;

    public BaseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        this.unitOfWork = unitOfWork;
        this.Mapper = mapper;
    }

    protected TRepo GetRepository<TRepo>()
        where TRepo : class, IBaseRepository
    {
        return unitOfWork.GetRepository<TRepo>();
    }

    protected IBaseRepository<TEntity> GetGenericRepository<TEntity>()
       where TEntity : class, IEntity
    {
        return unitOfWork.GetGenericRepository<TEntity>();
    }

    protected IBaseRepository<TEntity, TKey> GetGenericRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : struct
    {
        return unitOfWork.GetGenericRepository<TEntity, TKey>();
    }

    protected IDbContextTransaction CreateTransaction()
    {
        return this.unitOfWork.CreateTransaction();
    }

    protected IPaginationOutput<TModel> MapPagination<TEntity, TModel>(IPaginationOutput<TEntity> paginationOutput)
            where TEntity : class, IEntity
            where TModel : class
    {
        return new PaginationOutput<TModel>()
        {
            Items = this.Mapper.Map<IEnumerable<TModel>>(paginationOutput.Items),
            Page = paginationOutput.Page,
            Total = paginationOutput.Total,
            PageSize = paginationOutput.PageSize,
        };
    }
}