using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ResortQr.Application.Identity;

namespace ResortQr.UnitTests.TestDoubles;

/// <summary>Kho user giả (in-memory) cho test auth.</summary>
public sealed class FakeUserAuthStore : IUserAuthStore
{
    private readonly Dictionary<Guid, AuthenticatedUser> _byId = [];

    public void Add(AuthenticatedUser user) => _byId[user.UserId] = user;

    public AuthenticatedUser? Get(Guid userId) => _byId.GetValueOrDefault(userId);

    public Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        foreach (var user in _byId.Values)
        {
            if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<AuthenticatedUser?>(user);
            }
        }

        return Task.FromResult<AuthenticatedUser?>(null);
    }

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
