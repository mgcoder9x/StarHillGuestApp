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

/// <summary>Use-case tạo phòng + issue token (SQLite THẬT). Bất biến: 1 token Active/phòng, Version=1, preview che.</summary>
public sealed class CreateRoomUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CreateRoomUseCase NewUseCase(AppSqliteHarness harness, AppDbContext ctx) =>
        new(new EfUnitOfWork(ctx), harness.CurrentUser, harness.Clock, new CryptoTokenGenerator());

    [Fact]
    public async Task Create_issues_single_active_token_version_1_with_masked_preview()
    {
        using var harness = new AppSqliteHarness(T0, actor: Guid.CreateVersion7());
        await harness.SeedResortAsync();

        Guid roomId;
        string token;
        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("101", "A", 3));
            Assert.True(result.IsSuccess);
            roomId = result.Value.RoomId;
            token = result.Value.Token;
            Assert.EndsWith("…", result.Value.TokenPreview, StringComparison.Ordinal);
            Assert.DoesNotContain(token[7..], result.Value.TokenPreview, StringComparison.Ordinal);
        }

        await using (var verify = harness.CreateContext())
        {
            var tokens = await verify.RoomQrTokens.Where(t => t.RoomId == roomId).ToListAsync();
            Assert.Single(tokens);
            Assert.Equal(RoomQrTokenStatus.Active, tokens[0].Status);
            Assert.Equal(1, tokens[0].Version);
            Assert.Equal(token, tokens[0].Token);
        }
    }

    [Fact]
    public async Task Create_with_duplicate_room_number_returns_validation_error()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        await using (var ctx = harness.CreateContext())
        {
            Assert.True((await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("202", null, null))).IsSuccess);
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("202", null, null));
            Assert.True(result.IsFailure);
            Assert.Equal("validation_error", result.Error!.Code);
        }
    }

    [Fact]
    public async Task Two_rooms_get_distinct_global_tokens()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        string t1, t2;
        await using (var ctx = harness.CreateContext())
        {
            t1 = (await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("301", null, null))).Value.Token;
        }

        await using (var ctx = harness.CreateContext())
        {
            t2 = (await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("302", null, null))).Value.Token;
        }

        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public async Task Create_after_soft_delete_same_number_is_allowed()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();

        Guid roomId;
        await using (var ctx = harness.CreateContext())
        {
            roomId = (await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("401", null, null))).Value.RoomId;
        }

        await using (var ctx = harness.CreateContext())
        {
            ctx.Rooms.Remove(await ctx.Rooms.SingleAsync(r => r.Id == roomId));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("401", null, null));
            Assert.True(result.IsSuccess);
        }
    }

    [Fact]
    public async Task Create_without_seeded_resort_fails()
    {
        using var harness = new AppSqliteHarness(T0);
        // KHÔNG seed resort.

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(harness, ctx).ExecuteAsync(new CreateRoomInput("501", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("qr_generation_failed", result.Error!.Code);
    }
}
