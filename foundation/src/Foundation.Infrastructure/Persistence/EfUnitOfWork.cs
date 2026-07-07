using System.Collections.Concurrent;
using Foundation.Application.Abstractions.Persistence;
using Foundation.SharedKernel.Entities;
using Foundation.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Foundation.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork EF: điểm ghi DB DUY NHẤT + giao dịch tường minh (all-or-nothing).
/// Bắt <see cref="DbUpdateConcurrencyException"/> (EF) → ném <see cref="ConcurrencyConflictException"/>
/// TRUNG LẬP để tầng Api map 409 mà KHÔNG phụ thuộc EF (giữ Api sạch).
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly FoundationDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public EfUnitOfWork(FoundationDbContext context) => _context = context;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : Entity =>
        (IRepository<TEntity>)_repositories.GetOrAdd(typeof(TEntity), _ => new EfRepository<TEntity>(_context));

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "Dữ liệu đã thay đổi bởi thao tác khác, vui lòng tải lại và thử lại.", ex);
        }
    }

    /// <summary>
    /// Chạy <paramref name="action"/> trong transaction: commit nếu thành công, rollback nếu lỗi.
    /// Dùng <see cref="Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy"/> để tương thích retry
    /// (Npgsql EnableRetryOnFailure) — không tự mở transaction ngoài strategy (sẽ ném).
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                var result = await action(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw new ConcurrencyConflictException(
                    "Dữ liệu đã thay đổi bởi thao tác khác, vui lòng tải lại và thử lại.", ex);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
