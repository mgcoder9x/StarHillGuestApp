using System.Linq.Expressions;
using System.Security.Claims;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Messaging.Contracts;
using Identity.Application.RefreshToken;
using Identity.Contracts.Events;
using Identity.Domain;
using Xunit;

namespace Identity.UnitTests;

/// <summary>
/// Guard hành vi cho rotation refresh-token (design §7.4, F5/F10) — test THUẦN use case với fakes (không DB).
/// Cũng là guard AD-041 (TTL 14 ngày) + F.2 (refresh nạp user → phát role, chặn user vô hiệu). Race đa-connection
/// thật + Postgres → RefreshRotationEmitsEventTests (Testcontainers).
/// </summary>
public sealed class RefreshAccessTokenUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid FamilyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // User chủ token (F.2: refresh nạp user theo UserId). Id sinh UUIDv7 (Entity) — dùng làm mốc so token sub.
    private static readonly IdentityUser ActiveUser = new()
    {
        Username = "admin",
        PasswordHash = "$argon2id$v=19$m=1,t=1,p=1$c2FsdA$aGFzaA",
        Role = UserRole.Admin,
        IsActive = true,
    };

    private static RefreshAccessTokenUseCase Build(
        FakeRefreshTokenStore store, PassThroughUnitOfWork uow, FakeOutboxWriter? outbox = null, FakeJwt? jwt = null) =>
        new(store, new FakeUserRepository(ActiveUser), uow, new FixedClock(Now),
            new FakeTokenGenerator(), jwt ?? new FakeJwt(), outbox ?? new FakeOutboxWriter());

    private static RefreshAccessTokenUseCase BuildWithUser(
        FakeRefreshTokenStore store, PassThroughUnitOfWork uow, IdentityUser? user) =>
        new(store, new FakeUserRepository(user), uow, new FixedClock(Now),
            new FakeTokenGenerator(), new FakeJwt(), new FakeOutboxWriter());

    private static RefreshTokenSnapshot Snapshot(DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null) =>
        new(TokenId, ActiveUser.Id, FamilyId, "old-hash", expiresAt, revokedAt);

    [Fact]
    public async Task Valid_token_rotates_and_returns_new_tokens_with_role()
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = true };
        var uow = new PassThroughUnitOfWork();
        var outbox = new FakeOutboxWriter { UnitOfWork = uow }; // để bắt thứ tự enqueue-trước-SaveChanges.
        var jwt = new FakeJwt();

        var result = await Build(store, uow, outbox, jwt).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token:" + ActiveUser.Id, result.Value.AccessToken);
        Assert.Equal("new-raw-token", result.Value.RefreshToken);
        Assert.Equal(Now.Add(RefreshAccessTokenUseCase.RefreshTokenLifetime), result.Value.RefreshTokenExpiresAt);
        Assert.True(store.ConsumeCalled);
        Assert.True(store.AddCalled);
        Assert.Equal(FamilyId, store.Added!.FamilyId); // token mới cùng family (rotation)
        Assert.Equal(ActiveUser.Id, store.Added.UserId);
        Assert.Null(store.Added.RevokedAt);
        Assert.Equal(1, uow.SaveChangesCalls);

        // F.2: access-token MANG claim role (không thì admin mất quyền sau refresh).
        Assert.Equal("admin", jwt.LastIdentity!.FindFirst("role")!.Value);

        // AD-057: rotation thành công EMIT UserTokenRefreshedIntegrationEvent + ENQUEUE TRƯỚC SaveChanges (CP6).
        var evt = Assert.IsType<UserTokenRefreshedIntegrationEvent>(Assert.Single(outbox.Enqueued));
        Assert.Equal(ActiveUser.Id, evt.UserId);
        Assert.Equal("identity.user_token_refreshed", evt.EventType);
        Assert.Equal(0, outbox.SaveChangesCallsAtEnqueue);
    }

    [Fact]
    public async Task Failed_rotations_do_not_emit_event()
    {
        var cases = new[]
        {
            new FakeRefreshTokenStore { ByHash = null },                                             // unknown
            new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddSeconds(-1)) },                     // expired
            new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1), revokedAt: Now.AddMinutes(-5)) }, // reuse
            new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = false },  // lost race
        };

        foreach (var store in cases)
        {
            var uow = new PassThroughUnitOfWork();
            var outbox = new FakeOutboxWriter();

            var result = await Build(store, uow, outbox).ExecuteAsync(new RefreshTokenCommand("raw"));

            Assert.True(result.IsFailure);
            Assert.Empty(outbox.Enqueued);
        }
    }

    [Fact]
    public void Refresh_token_lifetime_is_14_days()
    {
        Assert.Equal(TimeSpan.FromDays(14), RefreshAccessTokenUseCase.RefreshTokenLifetime);
    }

    [Fact]
    public async Task Unknown_token_hash_fails_without_consuming()
    {
        var store = new FakeRefreshTokenStore { ByHash = null };
        var uow = new PassThroughUnitOfWork();

        var result = await Build(store, uow).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.False(store.ConsumeCalled);
    }

    [Fact]
    public async Task Expired_token_fails_without_consuming()
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddSeconds(-1)) };
        var uow = new PassThroughUnitOfWork();

        var result = await Build(store, uow).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.False(store.ConsumeCalled);
    }

    [Fact]
    public async Task Revoked_token_reuse_revokes_family_and_fails()
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1), revokedAt: Now.AddMinutes(-5)) };
        var uow = new PassThroughUnitOfWork();

        var result = await Build(store, uow).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.True(store.RevokeFamilyCalled); // reuse-detection: thu hồi cả family
        Assert.False(store.ConsumeCalled);
    }

    [Fact]
    public async Task Losing_consume_race_fails_without_adding_new_token()
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = false };
        var uow = new PassThroughUnitOfWork();

        var result = await Build(store, uow).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.True(store.ConsumeCalled);
        Assert.False(store.AddCalled); // thua race → không thêm token mới
    }

    [Fact]
    public async Task Missing_user_fails_without_consuming() // F.2: user đã xóa → refresh chết
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = true };
        var uow = new PassThroughUnitOfWork();

        var result = await BuildWithUser(store, uow, user: null).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.False(store.ConsumeCalled); // không rotate cho user không còn tồn tại
    }

    [Fact]
    public async Task Inactive_user_fails_without_consuming() // F.2: user bị vô hiệu hoá → refresh chết (giết session)
    {
        var inactive = new IdentityUser
        {
            Username = "disabled",
            PasswordHash = "$argon2id$v=19$m=1,t=1,p=1$c2FsdA$aGFzaA",
            Role = UserRole.Admin,
            IsActive = false,
        };
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = true };
        var uow = new PassThroughUnitOfWork();

        var result = await BuildWithUser(store, uow, inactive).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_refresh_token", result.Error.Code);
        Assert.False(store.ConsumeCalled);
    }

    // ── Fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeRefreshTokenStore : IRefreshTokenStore
    {
        public RefreshTokenSnapshot? ByHash { get; init; }
        public bool ConsumeResult { get; init; } = true;
        public bool ConsumeCalled { get; private set; }
        public bool AddCalled { get; private set; }
        public bool RevokeFamilyCalled { get; private set; }
        public RefreshTokenSnapshot? Added { get; private set; }

        public Task<RefreshTokenSnapshot?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
            Task.FromResult(ByHash);

        public Task<bool> TryConsumeAsync(Guid tokenId, DateTimeOffset now, string reason, Guid replacedByTokenId, CancellationToken ct = default)
        {
            ConsumeCalled = true;
            return Task.FromResult(ConsumeResult);
        }

        public Task AddAsync(RefreshTokenSnapshot newToken, CancellationToken ct = default)
        {
            AddCalled = true;
            Added = newToken;
            return Task.CompletedTask;
        }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
        {
            RevokeFamilyCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository(IdentityUser? user) : IRepository<IdentityUser>
    {
        public ValueTask<IdentityUser?> FindByIdAsync(Guid id, CancellationToken ct = default) => new(user);

        public Task<IdentityUser?> FirstOrDefaultAsync(Expression<Func<IdentityUser, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task<bool> AnyAsync(Expression<Func<IdentityUser, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult(user is not null);

        public void Add(IdentityUser entity) => throw new NotSupportedException();
        public void Update(IdentityUser entity) => throw new NotSupportedException();
        public void Remove(IdentityUser entity) => throw new NotSupportedException();
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Enqueued { get; } = [];
        public PassThroughUnitOfWork? UnitOfWork { get; init; }
        public int SaveChangesCallsAtEnqueue { get; private set; }

        public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
        {
            SaveChangesCallsAtEnqueue = UnitOfWork?.SaveChangesCalls ?? 0;
            Enqueued.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(0);
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default) => action(ct);
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        public string NewToken(int byteLength = 32) => "new-raw-token";
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public ClaimsIdentity? LastIdentity { get; private set; }

        public string Issue(ClaimsIdentity identity)
        {
            LastIdentity = identity;
            return "access-token:" + identity.FindFirst("sub")?.Value;
        }
    }
}
