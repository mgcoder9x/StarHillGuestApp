using System;
using System.Threading.Tasks;
using ResortQr.Application.GuestAccess;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using ResortQr.Infrastructure.Security;
using ResortQr.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.GuestAccess;

/// <summary>
/// Use-case ResolveToken (SQLite THẬT) — ma trận edge-case 13 §7 (E1–E6, E11). Phần rủi ro cao nhất:
/// sai edge-case = rò rỉ dữ liệu giữa các lượt khách. Race đa-connection (E7a) → Testcontainers (hoãn).
/// </summary>
public sealed class ResolveTokenUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ResolveTokenUseCase NewUseCase(AppSqliteHarness h, AppDbContext ctx) =>
        new(new EfUnitOfWork(ctx), h.Clock, new CryptoTokenGenerator(), new Sha256GuestSessionKeyHasher());

    private static async Task<(Guid RoomId, string Token)> SeedRoomWithTokenAsync(
        AppSqliteHarness h,
        Guid resortId,
        string roomNumber,
        string token,
        RoomStatus roomStatus = RoomStatus.Active,
        RoomQrTokenStatus tokenStatus = RoomQrTokenStatus.Active)
    {
        await using var ctx = h.CreateContext();
        var room = new Room { ResortId = resortId, RoomNumber = roomNumber, Status = roomStatus };
        ctx.Rooms.Add(room);
        ctx.RoomQrTokens.Add(new RoomQrToken
        {
            RoomId = room.Id,
            Token = token,
            TokenPreview = "prev…",
            Status = tokenStatus,
            Version = 1,
            CreatedAt = h.Clock.UtcNow,
        });
        await ctx.SaveChangesAsync();
        return (room.Id, token);
    }

    [Fact] // E1
    public async Task Unknown_token_returns_qr_invalid_and_creates_nothing()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput("does-not-exist", null));
            Assert.True(result.IsFailure);
            Assert.Equal("qr_invalid", result.Error!.Code);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(0, await verify.GuestSessions.CountAsync());
            Assert.Equal(0, await verify.GuestVisits.CountAsync());
        }
    }

    [Fact] // E2
    public async Task Revoked_token_returns_qr_revoked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (_, token) = await SeedRoomWithTokenAsync(harness, resortId, "101", "tok-revoked", tokenStatus: RoomQrTokenStatus.Revoked);

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));

        Assert.True(result.IsFailure);
        Assert.Equal("qr_revoked", result.Error!.Code);
    }

    [Theory] // E3
    [InlineData(RoomStatus.Inactive)]
    [InlineData(RoomStatus.Maintenance)]
    public async Task Inactive_room_returns_room_inactive(RoomStatus status)
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (_, token) = await SeedRoomWithTokenAsync(harness, resortId, "102", "tok-inactive", roomStatus: status);

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));

        Assert.True(result.IsFailure);
        Assert.Equal("room_inactive", result.Error!.Code);
    }

    [Fact] // E3 — soft-deleted room
    public async Task Soft_deleted_room_returns_room_inactive()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (roomId, token) = await SeedRoomWithTokenAsync(harness, resortId, "103", "tok-deleted");

        await using (var ctx = harness.CreateContext())
        {
            ctx.Rooms.Remove(await ctx.Rooms.SingleAsync(r => r.Id == roomId));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));
            Assert.True(result.IsFailure);
            Assert.Equal("room_inactive", result.Error!.Code);
        }
    }

    [Fact] // E4
    public async Task New_device_creates_session_and_active_visit_and_issues_key()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (roomId, token) = await SeedRoomWithTokenAsync(harness, resortId, "201", "tok-201");

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));
            Assert.True(result.IsSuccess);
            Assert.Equal(roomId, result.Value.RoomId);
            Assert.Equal("201", result.Value.RoomNumber);
            Assert.NotNull(result.Value.IssuedSessionKey);
            Assert.NotEqual(Guid.Empty, result.Value.VisitId);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(1, await verify.GuestSessions.CountAsync());
            var visit = await verify.GuestVisits.SingleAsync();
            Assert.Equal(Domain.GuestAccess.GuestVisitStatus.Active, visit.Status);
            Assert.Equal(roomId, visit.RoomId);
        }
    }

    [Fact] // E5
    public async Task Same_device_and_room_within_idle_resumes_same_visit_without_new_key()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (_, token) = await SeedRoomWithTokenAsync(harness, resortId, "202", "tok-202");

        string key;
        Guid firstVisit;
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));
            key = r.Value.IssuedSessionKey!;
            firstVisit = r.Value.VisitId;
        }

        harness.Clock.UtcNow = T0.AddHours(1); // vẫn trong idle 24h
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, key));
            Assert.Equal(firstVisit, r.Value.VisitId);   // nối lại ĐÚNG visit
            Assert.Null(r.Value.IssuedSessionKey);        // KHÔNG issue key mới
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(1, await verify.GuestVisits.CountAsync());
            var visit = await verify.GuestVisits.SingleAsync();
            Assert.Equal(T0.AddHours(1), visit.LastSeenAt);          // sliding window refresh
            Assert.Equal(T0.AddHours(1).AddHours(24), visit.ExpiresAt);
        }
    }

    [Fact] // E6
    public async Task Same_device_after_idle_expiry_expires_old_and_creates_new_visit()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (_, token) = await SeedRoomWithTokenAsync(harness, resortId, "203", "tok-203");

        string key;
        Guid firstVisit;
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, null));
            key = r.Value.IssuedSessionKey!;
            firstVisit = r.Value.VisitId;
        }

        harness.Clock.UtcNow = T0.AddHours(25); // quá idle 24h (sweeper chưa chạy)
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(token, key));
            Assert.True(r.IsSuccess);
            Assert.NotEqual(firstVisit, r.Value.VisitId); // visit MỚI
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(2, await verify.GuestVisits.CountAsync());
            var old = await verify.GuestVisits.SingleAsync(v => v.Id == firstVisit);
            Assert.Equal(Domain.GuestAccess.GuestVisitStatus.Expired, old.Status);
            Assert.NotNull(old.ClosedAt);
        }
    }

    [Fact] // E11
    public async Task Same_device_two_rooms_yields_two_active_visits()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var (_, tokenA) = await SeedRoomWithTokenAsync(harness, resortId, "301", "tok-A");
        var (_, tokenB) = await SeedRoomWithTokenAsync(harness, resortId, "302", "tok-B");

        string key;
        Guid visitA;
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(tokenA, null));
            key = r.Value.IssuedSessionKey!;
            visitA = r.Value.VisitId;
        }

        Guid visitB;
        await using (var ctx = harness.CreateContext())
        {
            var r = await NewUseCase(harness, ctx).ExecuteAsync(new ResolveTokenInput(tokenB, key));
            visitB = r.Value.VisitId;
        }

        Assert.NotEqual(visitA, visitB);
        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(2, await verify.GuestVisits.CountAsync(v => v.Status == Domain.GuestAccess.GuestVisitStatus.Active));
            Assert.Equal(1, await verify.GuestSessions.CountAsync());
        }
    }
}
