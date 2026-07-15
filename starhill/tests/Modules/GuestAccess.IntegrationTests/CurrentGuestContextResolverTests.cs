using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using GuestAccess.Application;
using GuestAccess.Contracts;
using GuestAccess.Domain;
using GuestAccess.Infrastructure.DependencyInjection;
using GuestAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;
using Testcontainers.PostgreSql;
using Xunit;

namespace GuestAccess.IntegrationTests;

/// <summary>
/// C-GA.4 (QR-AD-032) — <see cref="ICurrentGuestContextResolver"/> PORTAL-WINDOW check-before-touch trên PostgreSQL
/// thật (migration). ResolveAsync CHỈ đọc + kiểm window (KHÔNG touch); TouchAsync mới trượt cửa sổ (bảo toàn idle
/// delta). Phủ: còn hạn→context (không touch); quá window→session_expired; visit không Active→session_expired; cookie
/// rỗng/lạ→guest_context_missing; cấu hình thiếu→configuration_unavailable; Touch→trượt LastSeenAt + giữ delta idle.
/// SKIP nếu thiếu Docker (N-067). Cross-module config dùng stub (impl EF đã test ở ResortConfig).
/// </summary>
public sealed class CurrentGuestContextResolverTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker → skip.
        catch (Exception)
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_available)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    private const int PortalWindowMinutes = 30;
    private const int VisitIdleExpiryHours = 24;

    private (ServiceProvider Provider, MutableClock Clock, StubResortGuestConfigQuery Config) Build()
    {
        var clock = new MutableClock();
        var config = new StubResortGuestConfigQuery();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ITokenGenerator>(new SequentialTokenGenerator());
        services.AddSingleton<IRoomTokenResolver>(new StubRoomTokenResolver());
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddGuestAccessInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "guest_access")));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return (provider, clock, config);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId) => new(
        resortId, "Star Hill", null, ["en"], "en",
        true, true, true, true, true, true, PortalWindowMinutes, VisitIdleExpiryHours);

    // Canonical cookie (43 base64url). Seed session (hash cookie) + một visit với LastSeenAt cho trước.
    private const string Cookie = "abcdefghijklmnopqrstuvwxyz0123456789-_ABCDE";

    private static async Task<(Guid VisitId, Guid SessionId, Guid RoomId, Guid ResortId)> SeedAsync(
        ServiceProvider provider, DateTimeOffset lastSeenAt, GuestVisitStatus status)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IGuestSessionKeyHasher>();

        var session = new GuestSession
        {
            SessionKeyHash = hasher.Hash(Cookie),
            FirstSeenAt = lastSeenAt,
            LastSeenAt = lastSeenAt,
        };
        db.GuestSessions.Add(session);

        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visit = new GuestVisit
        {
            ResortId = resortId,
            RoomId = roomId,
            GuestSessionId = session.Id,
            Status = status,
            StartedAt = lastSeenAt,
            LastSeenAt = lastSeenAt,
            ExpiresAt = lastSeenAt.AddHours(VisitIdleExpiryHours),
            // Check constraint ck_guest_visit_closed_at: visit không-Active PHẢI có ClosedAt.
            ClosedAt = status == GuestVisitStatus.Active ? null : lastSeenAt,
        };
        db.GuestVisits.Add(visit);
        await db.SaveChangesAsync();
        return (visit.Id, session.Id, roomId, resortId);
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        await db.Database.MigrateAsync();
    }

    private static async Task<GuestVisit> ReadVisitAsync(ServiceProvider provider, Guid visitId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        return await db.GuestVisits.AsNoTracking().SingleAsync(v => v.Id == visitId);
    }

    private static async Task<Bedrock.Domain.Results.Result<CurrentGuestContext>> ResolveAsync(
        ServiceProvider provider, string? cookie, Guid roomId)
    {
        await using var scope = provider.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICurrentGuestContextResolver>();
        return await resolver.ResolveAsync(cookie, roomId);
    }

    [SkippableFact]
    public async Task Resolve_within_portal_window_returns_context_without_touching()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, config) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var lastSeen = clock.UtcNow.AddMinutes(-10); // trong 30' window
        var (visitId, sessionId, roomId, resortId) = await SeedAsync(provider, lastSeen, GuestVisitStatus.Active);
        config.Config = ConfigFor(resortId);

        var result = await ResolveAsync(provider, Cookie, roomId);

        Assert.True(result.IsSuccess);
        Assert.Equal(visitId, result.Value.GuestVisitId);
        Assert.Equal(sessionId, result.Value.GuestSessionId);
        Assert.Equal(roomId, result.Value.RoomId);
        Assert.Equal(resortId, result.Value.ResortId);

        // KHÔNG touch: LastSeenAt giữ nguyên (check-before-touch).
        var after = await ReadVisitAsync(provider, visitId);
        Assert.Equal(lastSeen, after.LastSeenAt);
    }

    [SkippableFact]
    public async Task Resolve_past_portal_window_returns_session_expired_without_touching()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, config) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var lastSeen = clock.UtcNow.AddMinutes(-31); // quá 30' window
        var (visitId, _, roomId, resortId) = await SeedAsync(provider, lastSeen, GuestVisitStatus.Active);
        config.Config = ConfigFor(resortId);

        var result = await ResolveAsync(provider, Cookie, roomId);

        Assert.False(result.IsSuccess);
        Assert.Equal("session_expired", result.Error.Code);
        var after = await ReadVisitAsync(provider, visitId);
        Assert.Equal(lastSeen, after.LastSeenAt); // không touch khi hết hạn
    }

    [SkippableFact]
    public async Task Resolve_non_active_visit_returns_session_expired()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, config) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var lastSeen = clock.UtcNow.AddMinutes(-5);
        var (_, _, roomId, resortId) = await SeedAsync(provider, lastSeen, GuestVisitStatus.Closed);
        config.Config = ConfigFor(resortId);

        var result = await ResolveAsync(provider, Cookie, roomId);

        Assert.False(result.IsSuccess);
        Assert.Equal("session_expired", result.Error.Code);
    }

    [SkippableFact]
    public async Task Resolve_unknown_cookie_returns_guest_context_missing()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, config) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var (_, _, roomId, resortId) = await SeedAsync(provider, clock.UtcNow.AddMinutes(-5), GuestVisitStatus.Active);
        config.Config = ConfigFor(resortId);

        var result = await ResolveAsync(provider, "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", roomId);

        Assert.False(result.IsSuccess);
        Assert.Equal("guest_context_missing", result.Error.Code);
    }

    [SkippableFact]
    public async Task Resolve_null_cookie_returns_guest_context_missing()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, _, _) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var result = await ResolveAsync(provider, null, Guid.CreateVersion7());

        Assert.False(result.IsSuccess);
        Assert.Equal("guest_context_missing", result.Error.Code);
    }

    [SkippableFact]
    public async Task Resolve_missing_config_returns_configuration_unavailable()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, config) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var (_, _, roomId, _) = await SeedAsync(provider, clock.UtcNow.AddMinutes(-5), GuestVisitStatus.Active);
        config.Config = null; // fail-closed

        var result = await ResolveAsync(provider, Cookie, roomId);

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [SkippableFact]
    public async Task Touch_slides_last_seen_and_preserves_idle_delta()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, _) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var lastSeen = clock.UtcNow.AddMinutes(-10);
        var (visitId, _, _, _) = await SeedAsync(provider, lastSeen, GuestVisitStatus.Active);

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<ICurrentGuestContextResolver>();
            await resolver.TouchAsync(visitId);
        }

        var after = await ReadVisitAsync(provider, visitId);
        Assert.Equal(clock.UtcNow, after.LastSeenAt); // trượt về now
        Assert.Equal(clock.UtcNow.AddHours(VisitIdleExpiryHours), after.ExpiresAt); // giữ nguyên delta idle (24h)
    }

    [SkippableFact]
    public async Task Touch_is_noop_on_non_active_visit()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var (provider, clock, _) = Build();
        await using var _ = provider;
        await Migrate(provider);

        var lastSeen = clock.UtcNow.AddMinutes(-10);
        var (visitId, _, _, _) = await SeedAsync(provider, lastSeen, GuestVisitStatus.Closed);

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<ICurrentGuestContextResolver>();
            await resolver.TouchAsync(visitId);
        }

        var after = await ReadVisitAsync(provider, visitId);
        Assert.Equal(lastSeen, after.LastSeenAt); // không hồi sinh phiên đã đóng
    }
}
