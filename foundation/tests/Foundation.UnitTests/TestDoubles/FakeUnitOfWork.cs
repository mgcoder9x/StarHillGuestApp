using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation.Application.Abstractions.Persistence;
using Foundation.SharedKernel.Entities;

namespace Foundation.UnitTests.TestDoubles;

/// <summary>UnitOfWork giả: SaveChanges no-op (fake store áp thay đổi ngay); transaction chạy thẳng action.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : Entity =>
        throw new NotSupportedException("FakeUnitOfWork không cung cấp Repository trong test này.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(0);
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action(cancellationToken);
    }
}
