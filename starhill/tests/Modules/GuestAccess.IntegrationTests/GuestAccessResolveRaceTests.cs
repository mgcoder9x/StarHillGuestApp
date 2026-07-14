using System.Collections.Concurrent;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using GuestAccess.Application;
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
/// RACE test (Testcontainers/PostgreSQL) cho QR-AD-026: nhiều resolve ĐỒNG THỜI cùng cookie đua TẠO visit cho một
/// phòng chưa có visit Active → khóa hàng session (<c>FOR UPDATE</c>) SERIALIZE chúng → HỘI TỤ về đúng MỘT visit
/// Active + cùng VisitId, KHÔNG 500 / KHÔNG vi phạm partial unique. SKIP nếu thiếu Docker (N-067). Cross-module
/// dùng stub (cô lập GuestAccess) — chỉ GuestAccessDbContext là Postgres thật (nơi có row-lock + partial unique).
/// </summary>
public sealed class GuestAccessResolveRaceTests : IAsyncLifetime
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private (ServiceProvider Provider, StubRoomTokenResolver Room) Build(StubResortGuestConfigQuery config)
    {
        var room = new StubRoomTokenResolver();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITokenGenerator>(new SequentialTokenGenerator());
        services.AddSingleton<IRoomTokenResolver>(room);
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddGuestAccessInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "guest_access")));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return (provider, room);
    }

    // Token phải canonical (43 base64url) để qua guard input; stub resolver bỏ qua GIÁ TRỊ token và trả theo
    // room.Result nên một token canonical dùng chung là đủ cho cả hai phòng.
    private static readonly string CanonicalToken = new('a', 43);

    private static async Task<Bedrock.Domain.Results.Result<ResolveTokenResult>> ResolveAsync(
        ServiceProvider provider, string? cookie)
    {
        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUseCase<ResolveTokenInput, ResolveTokenResult>>();
        return await useCase.ExecuteAsync(new ResolveTokenInput(CanonicalToken, cookie)).ConfigureAwait(false);
    }

    [SkippableFact]
    public async Task Concurrent_resolves_same_cookie_and_room_converge_to_single_active_visit()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        var resortId = Guid.CreateVersion7();
        var config = new StubResortGuestConfigQuery
        {
            Config = new ResortGuestConfig(
                resortId, "Star Hill", null, ["en"], "en",
                true, true, true, true, true, true, 30, 24),
        };
        var (provider, room) = Build(config);
        await using var _ = provider;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
            await db.Database.MigrateAsync();
        }

        // (1) Resolve lần đầu cho room1 → tạo session, lấy cookie. (visit room1 không liên quan tới đua room2).
        var roomId1 = Guid.CreateVersion7();
        room.Result = new RoomResolution(roomId1, resortId, "A-101", null, null, true);
        var firstResult = await ResolveAsync(provider, null);
        Assert.True(firstResult.IsSuccess);
        var cookie = firstResult.Value.IssuedSessionKey;
        Assert.NotNull(cookie);

        // (2) BURST: N resolve đồng thời cùng cookie cho room2 (CHƯA có visit) → đua TẠO visit.
        var roomId2 = Guid.CreateVersion7();
        room.Result = new RoomResolution(roomId2, resortId, "A-102", null, null, true);

        const int concurrency = 8;
        var visitIds = new ConcurrentBag<Guid>();
        var failures = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
        {
            var result = await ResolveAsync(provider, cookie).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                visitIds.Add(result.Value.VisitId);
            }
            else
            {
                failures.Add(result.Error.Code);
            }
        });
        await Task.WhenAll(tasks);

        // KHÔNG request nào lỗi (không 500/không vi phạm unique lọt ra).
        Assert.Empty(failures);
        Assert.Equal(concurrency, visitIds.Count);
        // HỘI TỤ: tất cả CÙNG một VisitId.
        Assert.Single(visitIds.Distinct());

        // DB: đúng MỘT visit Active cho (session, room2).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
            var session = await db.GuestSessions.AsNoTracking().SingleAsync();
            var activeForRoom2 = await db.GuestVisits
                .AsNoTracking()
                .CountAsync(v => v.GuestSessionId == session.Id && v.RoomId == roomId2 && v.Status == GuestVisitStatus.Active);
            Assert.Equal(1, activeForRoom2);
        }
    }
}
