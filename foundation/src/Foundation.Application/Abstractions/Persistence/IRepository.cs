using System.Linq.Expressions;
using Foundation.SharedKernel.Entities;

namespace Foundation.Application.Abstractions.Persistence;

/// <summary>
/// Repository generic: CHỈ thao tác ChangeTracker (Add/Update/Remove/Query) — KHÔNG tự ghi DB.
/// Điểm ghi DB duy nhất là <see cref="IUnitOfWork.SaveChangesAsync"/> (đảm bảo ghi nguyên tử liên bảng).
/// Truy vấn phức tạp/read-model nên đi qua query service riêng (tránh leaky IQueryable ra ngoài use case).
/// </summary>
public interface IRepository<TEntity> where TEntity : Entity
{
    IQueryable<TEntity> Query();

    ValueTask<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void Update(TEntity entity);

    /// <summary>Xóa; nếu entity là <see cref="ISoftDeletable"/> thì Infrastructure chuyển thành xóa mềm.</summary>
    void Remove(TEntity entity);
}
