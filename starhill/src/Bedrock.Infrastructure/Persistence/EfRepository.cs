using System.Linq.Expressions;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence;

/// <summary>
/// Repository EF generic cho aggregate GHI (design §5.1 / F9). CHỈ thao tác ChangeTracker + tra cứu
/// theo khóa/điều kiện; KHÔNG phơi <see cref="IQueryable{T}"/>. KHÔNG tự ghi DB — điểm ghi duy nhất là
/// <see cref="EfUnitOfWork.SaveChangesAsync"/>. Đăng ký Scoped cùng scope với <see cref="PlatformDbContext"/>
/// (một <c>DbContext</c>/scope) → mọi thay đổi vào chung một lần ghi.
/// </summary>
public sealed class EfRepository<T>(PlatformDbContext context) : IRepository<T>
    where T : Entity
{
    private DbSet<T> Set => context.Set<T>();

    public ValueTask<T?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FindAsync([id], ct);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set.FirstOrDefaultAsync(predicate, ct);
    }

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set.AnyAsync(predicate, ct);
    }

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Add(entity);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // Nếu entity là ISoftDeletable → PlatformDbContext chuyển Deleted → xóa mềm ở SaveChanges.
        Set.Remove(entity);
    }
}
