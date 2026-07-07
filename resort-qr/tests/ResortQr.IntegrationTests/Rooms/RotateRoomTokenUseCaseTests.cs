using System;
using System.Linq;
using System.Threading.Tasks;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using ResortQr.Infrastructure.Security;
using ResortQr.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Rooms;

/// <summary>Use-case rotate token (SQLite THẬT). Bất biến B2: nguyên tử, đúng 1 Active, token cũ giữ Revoked.</summary>
public sealed class RotateRoomTokenUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CreateRoomUseCase NewCreate(AppSqliteHarness h, AppDbContext ctx) =>
        new(new EfUnitOfWork(ctx), h.CurrentUser, h.Clock, new CryptoTokenGenerator());

    private static RotateRoomTokenUseCase NewRotate(AppSqliteHarness h, AppDbContext ctx) =>
        new(new EfUnitOfWork(ctx), h.CurrentUser, h.Clock, new CryptoTokenGenerator());

    private static async Task<Guid> CreateRoomAsync(AppSqliteHarness h, string number)
    {
        await using var ctx = h.CreateContext();
        return (await NewCreate(h, ctx).ExecuteAsync(new CreateRoomInput(number, null, null))).Value.RoomId;
    }

    [Fact]
    public async Task Rotate_revokes_old_and_issues_new_active_version_incremented()
    {
        using var harness = new AppSqliteHarness(T0, actor: Guid.CreateVersion7());
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "101");

        string newToken;
        await using (var ctx = harness.CreateContext())
        {
            var result = await NewRotate(harness, ctx).ExecuteAsync(new RotateRoomTokenInput(roomId, "reprint"));
            Assert.True(result.IsSuccess);
            newToken = result.Value.Token;
        }

        await using (var verify = harness.CreateContext())
        {
            var tokens = await verify.RoomQrTokens.Where(t => t.RoomId == roomId).OrderBy(t => t.Version).ToListAsync();
            Assert.Equal(2, tokens.Count);

            var revoked = tokens[0];
            Assert.Equal(RoomQrTokenStatus.Revoked, revoked.Status);
            Assert.NotNull(revoked.RevokedAt);
            Assert.Equal("reprint", revoked.RevocationReason);

            var active = tokens[1];
            Assert.Equal(RoomQrTokenStatus.Active, active.Status);
            Assert.Equal(2, active.Version);
            Assert.Equal(newToken, active.Token);

            Assert.Equal(1, tokens.Count(t => t.Status == RoomQrTokenStatus.Active));
        }
    }

    [Fact]
    public async Task Rotate_nonexistent_room_returns_not_found()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        await using var ctx = harness.CreateContext();
        var result = await NewRotate(harness, ctx).ExecuteAsync(new RotateRoomTokenInput(Guid.CreateVersion7(), null));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Rotate_inactive_room_returns_qr_generation_failed()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "102");

        await using (var ctx = harness.CreateContext())
        {
            var room = await ctx.Rooms.SingleAsync(r => r.Id == roomId);
            room.Status = RoomStatus.Inactive;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewRotate(harness, ctx).ExecuteAsync(new RotateRoomTokenInput(roomId, null));
            Assert.True(result.IsFailure);
            Assert.Equal("qr_generation_failed", result.Error!.Code);
        }
    }
}
