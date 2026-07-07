using System.Collections.Concurrent;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.SharedKernel.Entities;
using ResortQr.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork EF: điểm ghi DB DUY NHẤT + giao dịch tường minh (all-or-nothing).
/// Bắt <see cref="DbUpdateConcurrencyException"/> → <see cref="ConcurrencyConflictException"/> (409) và
/// unique-violation (<see cref="DbUpdateException"/> inner provider) → <see cref="UniqueConstraintViolationException"/>,
/// đều TRUNG LẬP để tầng Application/Api map lỗi mà KHÔNG phụ thuộc EF (giữ tầng trên sạch).
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ResortQrDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public EfUnitOfWork(ResortQrDbContext context) => _context = context;

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
        catch (DbUpdateException ex) when (TryMapUniqueViolation(ex, out var unique))
        {
            throw unique;
        }
    }

    /// <summary>
    /// Nhận diện unique-violation theo provider (fix gốc: EF không có abstraction chung).
    /// Npgsql: <see cref="PostgresException"/> SqlState 23505 + ConstraintName. SQLite: inner
    /// <c>SqliteException</c> với message "UNIQUE constraint failed" (Infrastructure KHÔNG ref Microsoft.Data.Sqlite
    /// nên nhận diện qua type-name + message — path test). Trả false với lỗi DB khác (rethrow nguyên trạng).
    /// </summary>
    private static bool TryMapUniqueViolation(DbUpdateException ex, out UniqueConstraintViolationException mapped)
    {
        switch (ex.InnerException)
        {
            case PostgresException pg when pg.SqlState == PostgresErrorCodes.UniqueViolation:
                mapped = new UniqueConstraintViolationException(
                    "Vi phạm ràng buộc duy nhất.", pg.ConstraintName, ex);
                return true;

            case { } inner when inner.GetType().FullName == "Microsoft.Data.Sqlite.SqliteException"
                                && inner.Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal):
                mapped = new UniqueConstraintViolationException("Vi phạm ràng buộc duy nhất.", constraintName: null, ex);
                return true;

            default:
                mapped = null!;
                return false;
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
            catch (DbUpdateException ex) when (TryMapUniqueViolation(ex, out var unique))
            {
                await transaction.RollbackAsync(ct).ConfigureAwait(false);
                throw unique;
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
