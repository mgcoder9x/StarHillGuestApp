using System.Security.Claims;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Messaging.Contracts;
using Identity.Application.RefreshToken;
using Identity.Contracts.Events;
using Xunit;

namespace Identity.UnitTests;

/// <summary>
/// Guard hành vi cho rotation refresh-token (design §7.4, F5/F10) — test THUẦN use case với fakes (không DB).
/// Cũng là guard AD-041 (TTL 14 ngày). Race đa-connection thật + Postgres → task 8.3 (Testcontainers).
/// </summary>
public sealed class RefreshAccessTokenUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FamilyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TokenId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static RefreshAccessTokenUseCase Build(
        FakeRefreshTokenStore store, PassThroughUnitOfWork uow, FakeOutboxWriter? outbox = null) =>
        new(store, uow, new FixedClock(Now), new FakeTokenGenerator(), new FakeJwt(), outbox ?? new FakeOutboxWriter());

    private static RefreshTokenSnapshot Snapshot(DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null) =>
        new(TokenId, UserId, FamilyId, "old-hash", expiresAt, revokedAt);

    [Fact]
    public async Task Valid_token_rotates_and_returns_new_tokens()
    {
        var store = new FakeRefreshTokenStore { ByHash = Snapshot(Now.AddDays(1)), ConsumeResult = true };
        var uow = new PassThroughUnitOfWork();
        var outbox = new FakeOutboxWriter { UnitOfWork = uow }; // để bắt thứ tự enqueue-trước-SaveChanges.

        var result = await Build(store, uow, outbox).ExecuteAsync(new RefreshTokenCommand("raw"));

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token:" + UserId, result.Value.AccessToken);
        Assert.Equal("new-raw-token", result.Value.RefreshToken);
        Assert.Equal(Now.Add(RefreshAccessTokenUseCase.RefreshTokenLifetime), result.Value.RefreshTokenExpiresAt);
        Assert.True(store.ConsumeCalled);
        Assert.True(store.AddCalled);
        Assert.Equal(FamilyId, store.Added!.FamilyId); // token mới cùng family (rotation)
        Assert.Equal(UserId, store.Added.UserId);
        Assert.Null(store.Added.RevokedAt);
        Assert.Equal(1, uow.SaveChangesCalls);

        // AD-057: rotation thành công EMIT UserTokenRefreshedIntegrationEvent vào outbox (đúng UserId + EventType)
        // và ENQUEUE TRƯỚC SaveChanges (cùng transaction → CP6 all-or-nothing). KHÔNG rò refresh token thô.
        var evt = Assert.IsType<UserTokenRefreshedIntegrationEvent>(Assert.Single(outbox.Enqueued));
        Assert.Equal(UserId, evt.UserId);
        Assert.Equal("identity.user_token_refreshed", evt.EventType);
        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.Equal(Now, evt.OccurredAt);
        Assert.Equal(0, outbox.SaveChangesCallsAtEnqueue); // enqueue xảy ra TRƯỚC SaveChanges.
    }

    [Fact]
    public async Task Failed_rotations_do_not_emit_event()
    {
        // Không token / hết hạn / reuse-revoked / thua race → KHÔNG được phát event (chỉ phát trên đường thành công).
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

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Enqueued { get; } = [];

        /// <summary>Liên kết tuỳ chọn để bắt thứ tự: số lần SaveChanges TẠI thời điểm enqueue (kỳ vọng 0).</summary>
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
        public string Issue(ClaimsIdentity identity) => "access-token:" + identity.FindFirst("sub")?.Value;
    }
}
