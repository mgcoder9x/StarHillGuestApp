using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Domain;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace Rooms.IntegrationTests;

/// <summary>
/// Kiểm <see cref="RenderRoomQrPngUseCase"/> trên SQLite in-memory + QRCoder THẬT (PNG thuần managed → CHẠY CỤC BỘ,
/// không cần Docker). Bao phủ: render PNG hợp lệ (chữ ký PNG) cho phòng Active + baseUrl https; nhánh lỗi cấu hình
/// (null/http/không-url → invalid_configuration); phòng Inactive → qr_generation_failed; phòng lạ → not_found;
/// phòng Active nhưng không còn token Active → qr_generation_failed. <see cref="IResortSettingsQuery"/> được STUB
/// (cô lập module — impl EF thật đã test ở ResortConfig): use case chỉ phụ thuộc Contracts nên stub là hợp lệ.
/// </summary>
public sealed class RenderRoomQrPngUseCaseTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequentialTokenGenerator : ITokenGenerator
    {
        private int _counter;
        public string NewToken(int byteLength = 32) =>
            $"tok-{Interlocked.Increment(ref _counter):D4}-{Guid.NewGuid():N}";
    }

    /// <summary>Stub trả snapshot với <see cref="ResortSettingsSnapshot.GuestWebBaseUrl"/> cấu hình được (hoặc null).</summary>
    private sealed class StubResortSettingsQuery(string? baseUrl) : IResortSettingsQuery
    {
        public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) =>
            Task.FromResult<ResortSettingsSnapshot?>(new ResortSettingsSnapshot(
                ResortId: Guid.CreateVersion7(),
                FaqEnabled: true,
                ChatEnabled: true,
                HousekeepingEnabled: true,
                RequireRuleAckForFaq: false,
                RequireRuleAckForChat: false,
                RequireRuleAckForHousekeeping: false,
                PortalWindowMinutes: 30,
                VisitIdleExpiryHours: 24,
                GuestWebBaseUrl: baseUrl,
                MaxMessageLength: 2000,
                MessageRateLimitPerMinute: 20,
                HousekeepingRateLimitPerHour: 10));
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync(string? baseUrl)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITokenGenerator, SequentialTokenGenerator>();
        services.AddSingleton<IResortSettingsQuery>(new StubResortSettingsQuery(baseUrl));
        services.AddSingleton<IResortExistenceQuery>(new TestResortExistenceQuery());
        services.AddRoomsInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateRoomAsync(ServiceProvider provider, string number = "101")
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
        var result = await uc.ExecuteAsync(new CreateRoomInput(Guid.CreateVersion7(), number, null, null));
        Assert.True(result.IsSuccess);
        return result.Value.RoomId;
    }

    private static async Task<Result<RenderRoomQrPngResult>> RenderAsync(ServiceProvider provider, Guid roomId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>>();
        return await uc.ExecuteAsync(new RenderRoomQrPngInput(roomId));
    }

    [Fact]
    public async Task Renders_valid_png_for_active_room_with_https_baseurl()
    {
        var (provider, connection) = await BuildAsync("https://guest.starhill.local");
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);
        var result = await RenderAsync(provider, roomId);

        Assert.True(result.IsSuccess);
        var png = result.Value.Png;
        Assert.NotEmpty(png);
        Assert.Equal(PngSignature, png[..4]); // chữ ký PNG (‰PNG).
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://guest.local")]   // KHÔNG https.
    [InlineData("not-a-url")]            // không phải URL tuyệt đối.
    public async Task Invalid_baseurl_returns_invalid_configuration(string? baseUrl)
    {
        var (provider, connection) = await BuildAsync(baseUrl);
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);
        var result = await RenderAsync(provider, roomId);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.InvalidConfiguration.Code, result.Error.Code);
    }

    [Fact]
    public async Task Inactive_room_returns_qr_generation_failed()
    {
        var (provider, connection) = await BuildAsync("https://guest.starhill.local");
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);
        await using (var scope = provider.CreateAsyncScope())
        {
            var status = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ChangeRoomStatusInput>>();
            Assert.True((await status.ExecuteAsync(new ChangeRoomStatusInput(roomId, RoomStatus.Inactive))).IsSuccess);
        }

        var result = await RenderAsync(provider, roomId);
        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.QrGenerationFailed.Code, result.Error.Code);
    }

    [Fact]
    public async Task Unknown_room_returns_not_found()
    {
        var (provider, connection) = await BuildAsync("https://guest.starhill.local");
        await using var _ = provider;
        await using var __ = connection;

        var result = await RenderAsync(provider, Guid.CreateVersion7());
        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.RoomNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Active_room_without_active_token_returns_qr_generation_failed()
    {
        var (provider, connection) = await BuildAsync("https://guest.starhill.local");
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        // Thu hồi token Active thủ công (không rotate — rotate lại cấp token mới) → phòng Active nhưng KHÔNG còn token.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var token = await db.RoomQrTokens.SingleAsync(t => t.RoomId == roomId);
            token.Status = RoomQrTokenStatus.Revoked;
            token.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var result = await RenderAsync(provider, roomId);
        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.QrGenerationFailed.Code, result.Error.Code);
    }
}
