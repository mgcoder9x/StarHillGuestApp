using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ResortQr.Application.Identity;

namespace ResortQr.UnitTests.TestDoubles;

/// <summary>Kho refresh token giả (in-memory). Add/Update áp ngay; FindByHash trả cả bản ghi đã thu hồi (cho reuse-detection).</summary>
public sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<Guid, RefreshTokenRecord> _byId = [];
    private readonly object _gate = new();

    public IReadOnlyCollection<RefreshTokenRecord> Records => _byId.Values;

    public RefreshTokenRecord? GetByHash(string tokenHash) =>
        _byId.Values.FirstOrDefault(r => r.TokenHash == tokenHash);

    public Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(GetByHash(tokenHash));

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
        foreach (var record in _byId.Values)
        {
            if (record.FamilyId == familyId && record.RevokedAt is null)
            {
                record.RevokedAt = revokedAt;
                record.RevokedReason = reason;
            }
        }

        return Task.CompletedTask;
    }
}
