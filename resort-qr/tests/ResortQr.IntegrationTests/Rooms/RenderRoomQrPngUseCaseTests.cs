using System;
using System.Threading.Tasks;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using ResortQr.Infrastructure.Rooms;
using ResortQr.Infrastructure.Security;
using ResortQr.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Rooms;

/// <summary>Use-case render QR PNG (SQLite THẬT + QRCoder). Kiểm chữ ký PNG + các nhánh lỗi cấu hình/trạng thái.</summary>
public sealed class RenderRoomQrPngUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];

    private static async Task<Guid> SeedResortWithSettingsAsync(AppSqliteHarness h, string? baseUrl)
    {
        var resortId = await h.SeedResortAsync();
        await using var ctx = h.CreateContext();
        ctx.ResortSettingsSet.Add(new ResortSettings { ResortId = resortId, GuestWebBaseUrl = baseUrl });
        await ctx.SaveChangesAsync();
        return resortId;
    }

    private static async Task<Guid> CreateRoomAsync(AppSqliteHarness h, string number)
    {
        await using var ctx = h.CreateContext();
        var uc = new CreateRoomUseCase(new EfUnitOfWork(ctx), h.CurrentUser, h.Clock, new CryptoTokenGenerator());
        return (await uc.ExecuteAsync(new CreateRoomInput(number, null, null))).Value.RoomId;
    }

    private static RenderRoomQrPngUseCase NewUseCase(AppDbContext ctx) =>
        new(new EfUnitOfWork(ctx), new QrCoderQrService());

    [Fact]
    public async Task Renders_valid_png_for_active_room_with_https_baseurl()
    {
        using var harness = new AppSqliteHarness(T0);
        await SeedResortWithSettingsAsync(harness, "https://guest.local");
        var roomId = await CreateRoomAsync(harness, "101");

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(ctx).ExecuteAsync(new RenderRoomQrPngInput(roomId));

        Assert.True(result.IsSuccess);
        var png = result.Value.Png;
        Assert.NotEmpty(png);
        Assert.Equal(PngSignature, png[..4]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://guest.local")]  // không phải https
    [InlineData("not-a-url")]
    public async Task Invalid_baseurl_returns_invalid_configuration(string? baseUrl)
    {
        using var harness = new AppSqliteHarness(T0);
        await SeedResortWithSettingsAsync(harness, baseUrl);
        var roomId = await CreateRoomAsync(harness, "102");

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(ctx).ExecuteAsync(new RenderRoomQrPngInput(roomId));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_configuration", result.Error!.Code);
    }

    [Fact]
    public async Task Inactive_room_returns_qr_generation_failed()
    {
        using var harness = new AppSqliteHarness(T0);
        await SeedResortWithSettingsAsync(harness, "https://guest.local");
        var roomId = await CreateRoomAsync(harness, "103");

        await using (var ctx = harness.CreateContext())
        {
            var room = await ctx.Rooms.SingleAsync(r => r.Id == roomId);
            room.Status = RoomStatus.Inactive;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(ctx).ExecuteAsync(new RenderRoomQrPngInput(roomId));
            Assert.True(result.IsFailure);
            Assert.Equal("qr_generation_failed", result.Error!.Code);
        }
    }

    [Fact]
    public async Task Unknown_room_returns_not_found()
    {
        using var harness = new AppSqliteHarness(T0);
        await SeedResortWithSettingsAsync(harness, "https://guest.local");

        await using var ctx = harness.CreateContext();
        var result = await NewUseCase(ctx).ExecuteAsync(new RenderRoomQrPngInput(Guid.CreateVersion7()));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task Active_room_without_active_token_returns_qr_generation_failed()
    {
        using var harness = new AppSqliteHarness(T0);
        await SeedResortWithSettingsAsync(harness, "https://guest.local");
        var roomId = await CreateRoomAsync(harness, "104");

        // Thu hồi token Active thủ công → phòng Active nhưng KHÔNG còn token Active.
        await using (var ctx = harness.CreateContext())
        {
            var token = await ctx.RoomQrTokens.SingleAsync(t => t.RoomId == roomId);
            token.Status = RoomQrTokenStatus.Revoked;
            token.RevokedAt = T0;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var result = await NewUseCase(ctx).ExecuteAsync(new RenderRoomQrPngInput(roomId));
            Assert.True(result.IsFailure);
            Assert.Equal("qr_generation_failed", result.Error!.Code);
        }
    }
}
