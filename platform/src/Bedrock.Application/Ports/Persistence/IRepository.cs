using System.Linq.Expressions;
using Bedrock.Domain.Entities;

namespace Bedrock.Application.Ports.Persistence;

/// <summary>
/// Repository generic cho aggregate GHI: CHỈ thao tác ChangeTracker + tra cứu theo khóa/điều kiện.
/// <b>KHÔNG phơi <c>IQueryable</c></b> (F9): tránh rò chi tiết provider + query EF-specific ra use case.
/// Đọc phức tạp/đọc nhiều → dùng read-model/query service riêng (CQRS-lite) trả DTO.
/// KHÔNG tự ghi DB — điểm ghi duy nhất là <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public interface IRepository<T>
    where T : Entity
{
    ValueTask<T?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    void Add(T entity);

    /// <summary>Chỉ cần cho detached entity; entity đã track tự phát hiện thay đổi.</summary>
    void Update(T entity);

    /// <summary>Xóa; nếu entity là <see cref="ISoftDeletable"/> thì interceptor xử lý xóa mềm ở SaveChanges.</summary>
    void Remove(T entity);
}
