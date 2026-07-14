using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using GuestAccess.Application;
using GuestAccess.Domain;
using GuestAccess.Infrastructure.DependencyInjection;
using GuestAccess.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;
using Xunit;

namespace GuestAccess.IntegrationTests;

/// <summary>
/// Test LOGIC resolve trên SQLite in-memory (CHẠY CỤC BỘ, không Docker). Cô lập cross-module bằng stub
/// <see cref="IRoomTokenResolver"/>/<see cref="IResortGuestConfigQuery"/> (impl EF thật đã test ở Rooms/ResortConfig).
/// Phủ: qr_invalid / room_inactive / configuration_unavailable; tạo session+visit mới; nối lại visit; lazy
/// idle-expiry tạo visit mới; cửa sổ thao tác. Race PostgreSQL ở <c>GuestAccessResolveRaceTests</c>.
/// </summary>
public sealed class ResolveTokenUseCaseTests
{
    private static readonly string ValidToken = new('q', 43);

    private static ResortGuestConfig Config(Guid resortId) => new(
        resortId,
        "Star Hill",
        LogoUrl: null,
        EnabledLanguageCodes: ["en", "vi"],
        DefaultLanguageCode: "en",
        FaqEnabled: true,
        ChatEnabled: true,
        HousekeepingEnabled: true,
        RequireRuleAckForFaq: true,
        RequireRuleAckForChat: true,
        RequireRuleAckForHousekeeping: true,
        PortalWindowMinutes: 30,
        VisitIdleExpiryHours: 24);

    private static RoomResolution ActiveRoom(Guid roomId, Guid resortId) =>
        new(roomId, resortId, "A-101", "A", 1, IsRoomActive: true);

    private sealed record Harness(
        ServiceProvider Provider,
        SqliteConnection Connection,
        MutableClock Clock,
        StubRoomTokenResolver Room,
        StubResortGuestConfigQuery ConfigQuery) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private static async Task<Harness> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var clock = new MutableClock();
        var room = new StubRoomTokenResolver();
        var configQuery = new StubResortGuestConfigQuery();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ITokenGenerator>(new SequentialTokenGenerator());
        services.AddSingleton<IRoomTokenResolver>(room);
        services.AddSingleton<IResortGuestConfigQuery>(configQuery);
        services.AddGuestAccessInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new Harness(provider, connection, clock, room, configQuery);
    }

    private static async Task<Bedrock.Domain.Results.Result<ResolveTokenResult>> ResolveAsync(
        Harness h, string token, string? cookie)
    {
        await using var scope = h.Provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUseCase<ResolveTokenInput, ResolveTokenResult>>();
        return await useCase.ExecuteAsync(new ResolveTokenInput(token, cookie));
    }

    [Fact]
    public async Task Returns_qr_invalid_when_resolver_returns_null()
    {
        await using var h = await BuildAsync();
        h.Room.Result = null;

        var result = await ResolveAsync(h, ValidToken, null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.QrInvalid.Code, result.Error.Code);
    }

    [Fact]
    public async Task Returns_room_inactive_when_room_not_active()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = new RoomResolution(roomId, resortId, "A-101", null, null, IsRoomActive: false);
        h.ConfigQuery.Config = Config(resortId);

        var result = await ResolveAsync(h, ValidToken, null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.RoomInactive.Code, result.Error.Code);
    }

    [Fact]
    public async Task Returns_configuration_unavailable_when_config_missing()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = null; // fail-closed

        var result = await ResolveAsync(h, ValidToken, null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.ConfigurationUnavailable.Code, result.Error.Code);
    }

    [Fact]
    public async Task Creates_session_and_visit_for_new_device()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = Config(resortId);

        var result = await ResolveAsync(h, ValidToken, null);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.IssuedSessionKey); // session mới → cấp cookie.
        Assert.NotEqual(Guid.Empty, result.Value.VisitId);
        Assert.Equal("A-101", result.Value.RoomNumber);
        Assert.Equal("en", result.Value.DefaultLanguageCode);
        // Cửa sổ thao tác = now + PortalWindowMinutes (30').
        Assert.Equal(h.Clock.UtcNow.AddMinutes(30), result.Value.PortalWindowExpiresAt);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        Assert.Equal(1, await db.GuestSessions.CountAsync());
        Assert.Equal(1, await db.GuestVisits.CountAsync(v => v.Status == GuestVisitStatus.Active));
    }

    [Fact]
    public async Task Reconnects_same_active_visit_for_same_cookie_and_room()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = Config(resortId);

        var first = await ResolveAsync(h, ValidToken, null);
        var cookie = first.Value.IssuedSessionKey;
        Assert.NotNull(cookie);

        var second = await ResolveAsync(h, ValidToken, cookie);

        Assert.True(second.IsSuccess);
        Assert.Null(second.Value.IssuedSessionKey);            // nối lại session cũ → KHÔNG cấp cookie mới.
        Assert.Equal(first.Value.VisitId, second.Value.VisitId); // CÙNG visit.

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        Assert.Equal(1, await db.GuestSessions.CountAsync());
        Assert.Equal(1, await db.GuestVisits.CountAsync());
    }

    [Fact]
    public async Task Creates_new_visit_when_previous_visit_idle_expired()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = Config(resortId);

        var first = await ResolveAsync(h, ValidToken, null);
        var cookie = first.Value.IssuedSessionKey;

        // Vượt idle 24h → visit cũ Expired, tạo visit mới (Req 10.5).
        h.Clock.UtcNow = h.Clock.UtcNow.AddHours(25);
        var second = await ResolveAsync(h, ValidToken, cookie);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.VisitId, second.Value.VisitId);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        Assert.Equal(1, await db.GuestVisits.CountAsync(v => v.Status == GuestVisitStatus.Active));
        Assert.Equal(1, await db.GuestVisits.CountAsync(v => v.Status == GuestVisitStatus.Expired));
    }

    // QR-AD-025 (C-GA.3a): capability token do Rooms phát hành là base64url 43 ký tự. Mọi biến thể không canonical
    // phải trả qr_invalid TRƯỚC resolver/DB (không lộ lý do, không tốn I/O). Bao phủ null/rỗng/whitespace/quá
    // ngắn/quá dài/ký tự ngoài base64url (space, '+', '/', '=').
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short-token")]
    public async Task Rejects_noncanonical_token_before_resolver(string? token)
    {
        await using var h = await BuildAsync();
        h.Room.Result = ActiveRoom(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await ResolveAsync(h, token!, null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.QrInvalid.Code, result.Error.Code);
        Assert.Equal(0, h.Room.CallCount); // KHÔNG chạm resolver/DB khi token malformed.
    }

    [Theory]
    [InlineData(42)] // quá ngắn
    [InlineData(44)] // quá dài
    public async Task Rejects_token_with_wrong_length_before_resolver(int length)
    {
        await using var h = await BuildAsync();
        h.Room.Result = ActiveRoom(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await ResolveAsync(h, new string('q', length), null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.QrInvalid.Code, result.Error.Code);
        Assert.Equal(0, h.Room.CallCount);
    }

    [Theory]
    [InlineData('+')]
    [InlineData('/')]
    [InlineData('=')]
    [InlineData(' ')]
    public async Task Rejects_token_with_non_base64url_char_before_resolver(char invalid)
    {
        await using var h = await BuildAsync();
        h.Room.Result = ActiveRoom(Guid.CreateVersion7(), Guid.CreateVersion7());

        // 43 ký tự nhưng chứa một ký tự ngoài bảng base64url → vẫn phải bị loại.
        var token = new string('q', 42) + invalid;

        var result = await ResolveAsync(h, token, null);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestAccessErrors.QrInvalid.Code, result.Error.Code);
        Assert.Equal(0, h.Room.CallCount);
    }

    // QR-AD-025 (C-GA.3a): cookie không canonical (kể cả whitespace hoặc quá dài) được coi như thiết bị MỚI,
    // KHÔNG hash/lookup. Chứng minh bằng: resolve lần 1 tạo session; resolve lần 2 với cookie rác → session THỨ HAI
    // được cấp (IssuedSessionKey != null) và tổng số session = 2 (không nối lại, không ném từ hasher).
    [Theory]
    [InlineData("   ")]
    [InlineData("not a valid cookie value")]
    [InlineData("short")]
    public async Task Treats_noncanonical_cookie_as_new_session(string malformedCookie)
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = Config(resortId);

        var first = await ResolveAsync(h, ValidToken, null);
        Assert.NotNull(first.Value.IssuedSessionKey);

        var second = await ResolveAsync(h, ValidToken, malformedCookie);

        Assert.True(second.IsSuccess);
        Assert.NotNull(second.Value.IssuedSessionKey);                 // cookie rác → cấp session MỚI.
        Assert.NotEqual(first.Value.VisitId, second.Value.VisitId);    // visit khác vì session khác.

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        Assert.Equal(2, await db.GuestSessions.CountAsync());          // không nối lại → hai session độc lập.
    }

    // Cookie quá dài (vượt xa 43 ký tự) trước đây khiến hasher chạy; nay bị chuẩn hóa thành null trước hash/lookup.
    [Fact]
    public async Task Oversized_cookie_does_not_reconnect_and_issues_new_session()
    {
        await using var h = await BuildAsync();
        var roomId = Guid.CreateVersion7();
        var resortId = Guid.CreateVersion7();
        h.Room.Result = ActiveRoom(roomId, resortId);
        h.ConfigQuery.Config = Config(resortId);

        var first = await ResolveAsync(h, ValidToken, null);
        Assert.NotNull(first.Value.IssuedSessionKey);

        var oversized = new string('a', 8192);
        var second = await ResolveAsync(h, ValidToken, oversized);

        Assert.True(second.IsSuccess);
        Assert.NotNull(second.Value.IssuedSessionKey);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        Assert.Equal(2, await db.GuestSessions.CountAsync());
    }
}
