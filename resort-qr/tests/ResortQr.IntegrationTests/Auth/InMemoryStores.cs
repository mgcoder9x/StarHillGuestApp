using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Identity;
using ResortQr.SharedKernel.Entities;

namespace ResortQr.IntegrationTests.Auth;

/// <summary>Store user in-memory (singleton) cho integration test HTTP — thay EF (chưa có DB).</summary>
public sealed class InMemoryUserAuthStore : IUserAuthStore
{
    private readonly ConcurrentDictionary<Guid, AuthenticatedUser> _byId = new();

    public void Seed(AuthenticatedUser user) => _byId[user.UserId] = user;

    public Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<AuthenticatedUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(userId));

    public Task UpdatePasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        if (_byId.TryGetValue(userId, out var user))
        {
            _byId[userId] = user with { PasswordHash = passwordHash };
        }

        return Task.CompletedTask;
    }
}

/// <summary>Store refresh token in-memory (singleton) — giữ state qua nhiều request (login → refresh).</summary>
public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<Guid, RefreshTokenRecord> _byId = new();
    private readonly object _gate = new();

    public Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(r => r.TokenHash == tokenHash));

    public Task AddAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default)
    {
        _byId[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default)
    {
        _byId[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task<bool> TryConsumeAsync(Guid tokenId, DateTimeOffset revokedAt, string reason, Guid replacedByTokenId, CancellationToken cancellationToken = default)
    {
        // Atomic check-and-set: mô phỏng conditional UPDATE ... WHERE revoked_at IS NULL của EF impl.
        lock (_gate)
        {
            if (_byId.TryGetValue(tokenId, out var record) && record.RevokedAt is null)
            {
                record.RevokedAt = revokedAt;
                record.RevokedReason = reason;
                record.ReplacedByTokenId = replacedByTokenId;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAt, string reason, CancellationToken cancellationToken = default)
    {
        foreach (var record in _byId.Values.Where(r => r.FamilyId == familyId && r.RevokedAt is null))
        {
            record.RevokedAt = revokedAt;
            record.RevokedReason = reason;
        }

        return Task.CompletedTask;
    }
}

/// <summary>UnitOfWork no-op (store in-memory áp thay đổi ngay). Transaction chạy thẳng action.</summary>
public sealed class NoOpUnitOfWork : IUnitOfWork
{
    public IRepository<TEntity> Repository<TEntity>() where TEntity : Entity =>
        throw new NotSupportedException("Integration test không dùng generic Repository.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return action(cancellationToken);
    }
}
