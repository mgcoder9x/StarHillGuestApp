using Foundation.SharedKernel.Entities;

namespace Foundation.Application.Abstractions.Persistence;

/// <summary>
/// Điểm ghi DB DUY NHẤT + giao dịch tường minh. Gom nhiều thay đổi vào một lần ghi nguyên tử.
/// </summary>
public interface IUnitOfWork
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : Entity;

    /// <summary>Ghi tất cả thay đổi đang chờ trong MỘT lần. Trả số bản ghi bị ảnh hưởng.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Chạy <paramref name="action"/> trong một transaction: commit nếu không lỗi, rollback nếu lỗi (all-or-nothing).</summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default);
}
