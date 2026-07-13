using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rooms.Application;
using Rooms.Domain;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace Rooms.IntegrationTests;

/// <summary>
/// Kiểm use case module Rooms (B-Rooms.2b-i) trên SQLite in-memory (DB quan hệ thật, CHẠY CỤC BỘ — không cần Docker).
/// Bao phủ happy-path + nhánh lỗi provider-agnostic: Create→1 token Active/Version=1; Rotate→revoke cũ + Active mới +
/// Version++; Update/ChangeStatus/Delete(soft); not-found; rotate phòng không Active → qr_generation_failed.
/// Ánh xạ trùng số phòng → RoomNumberTaken cần dịch <c>UniqueConstraintViolationException</c> (Npgsql-only SqlState
/// 23505) nên test riêng trên Postgres — xem <see cref="RoomsPostgresConstraintTests"/>.
/// </summary>
public sealed class RoomsUseCaseTests
{
    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; init; } = Guid.CreateVersion7();
        public bool IsAuthenticated => UserId is not null;
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

    /// <summary>Token generator tất định-duy nhất cho test (mỗi lần gọi ra chuỗi mới, đủ dài để Mask cắt 6 ký tự).</summary>
    private sealed class SequentialTokenGenerator : ITokenGenerator
    {
        private int _counter;
        public string NewToken(int byteLength = 32) =>
            $"tok-{Interlocked.Increment(ref _counter):D4}-{Guid.NewGuid():N}";
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITokenGenerator, SequentialTokenGenerator>();
        services.AddSingleton<ResortConfig.Contracts.Queries.IResortExistenceQuery>(new TestResortExistenceQuery());
        services.AddRoomsInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateRoomAsync(ServiceProvider provider, string number = "A-101")
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
        var result = await uc.ExecuteAsync(new CreateRoomInput(Guid.CreateVersion7(), number, "A", 1));
        Assert.True(result.IsSuccess);
        return result.Value.RoomId;
    }

    // ─────────────────────── Create ───────────────────────
    [Fact]
    public async Task Create_persists_room_with_exactly_one_active_token()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        CreateRoomResult created;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
            var result = await uc.ExecuteAsync(new CreateRoomInput(Guid.CreateVersion7(), "A-101", "A", 1));
            Assert.True(result.IsSuccess);
            created = result.Value;
        }

        Assert.NotEqual(Guid.Empty, created.RoomId);
        Assert.False(string.IsNullOrEmpty(created.Token));
        Assert.EndsWith("…", created.TokenPreview, StringComparison.Ordinal); // Mask = 6 ký tự + "…".

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = await db.Rooms.SingleAsync();
            Assert.Equal(RoomStatus.Active, room.Status);

            var tokens = await db.RoomQrTokens.Where(t => t.RoomId == created.RoomId).ToListAsync();
            var active = Assert.Single(tokens, t => t.Status == RoomQrTokenStatus.Active);
            Assert.Equal(1, active.Version);
            Assert.Equal(created.Token, active.Token);
            Assert.NotNull(active.CreatedByUserId); // actor audit từ ICurrentUser.
        }
    }

    // ─────────────────────── Rotate ───────────────────────
    [Fact]
    public async Task Rotate_revokes_current_and_issues_new_active_with_incremented_version()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        RotateRoomTokenResult rotated;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>>();
            var result = await uc.ExecuteAsync(new RotateRoomTokenInput(roomId, "nghi ngờ lộ token"));
            Assert.True(result.IsSuccess);
            rotated = result.Value;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var tokens = await db.RoomQrTokens.Where(t => t.RoomId == roomId).OrderBy(t => t.Version).ToListAsync();
            Assert.Equal(2, tokens.Count); // giữ lịch sử — token cũ KHÔNG bị xóa.

            var revoked = Assert.Single(tokens, t => t.Status == RoomQrTokenStatus.Revoked);
            Assert.Equal(1, revoked.Version);
            Assert.NotNull(revoked.RevokedAt);
            Assert.Equal("nghi ngờ lộ token", revoked.RevocationReason);

            var active = Assert.Single(tokens, t => t.Status == RoomQrTokenStatus.Active);
            Assert.Equal(2, active.Version); // Version++.
            Assert.Equal(rotated.Token, active.Token);
        }
    }

    [Fact]
    public async Task Rotate_missing_room_returns_not_found()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>>();
        var result = await uc.ExecuteAsync(new RotateRoomTokenInput(Guid.CreateVersion7(), null));

        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.RoomNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Rotate_on_non_active_room_returns_qr_generation_failed()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        // Chuyển phòng sang Inactive rồi rotate → không được sinh token.
        await using (var scope = provider.CreateAsyncScope())
        {
            var status = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ChangeRoomStatusInput>>();
            Assert.True((await status.ExecuteAsync(new ChangeRoomStatusInput(roomId, RoomStatus.Inactive))).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>>();
            var result = await uc.ExecuteAsync(new RotateRoomTokenInput(roomId, null));
            Assert.True(result.IsFailure);
            Assert.Equal(RoomsErrors.QrGenerationFailed.Code, result.Error.Code);
        }
    }

    // ─────────────────────── Update / ChangeStatus / Delete ───────────────────────
    [Fact]
    public async Task Update_changes_room_fields()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateRoomInput>>();
            var result = await uc.ExecuteAsync(new UpdateRoomInput(roomId, "B-202", "B", 2));
            Assert.True(result.IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = await db.Rooms.SingleAsync(r => r.Id == roomId);
            Assert.Equal("B-202", room.RoomNumber);
            Assert.Equal("B", room.Building);
            Assert.Equal(2, room.Floor);
        }
    }

    [Fact]
    public async Task Update_missing_room_returns_not_found()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateRoomInput>>();
        var result = await uc.ExecuteAsync(new UpdateRoomInput(Guid.CreateVersion7(), "X-1", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.RoomNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ChangeStatus_updates_status_and_missing_room_returns_not_found()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ChangeRoomStatusInput>>();
            Assert.True((await uc.ExecuteAsync(new ChangeRoomStatusInput(roomId, RoomStatus.Maintenance))).IsSuccess);

            var missing = await uc.ExecuteAsync(new ChangeRoomStatusInput(Guid.CreateVersion7(), RoomStatus.Active));
            Assert.True(missing.IsFailure);
            Assert.Equal(RoomsErrors.RoomNotFound.Code, missing.Error.Code);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            Assert.Equal(RoomStatus.Maintenance, (await db.Rooms.SingleAsync(r => r.Id == roomId)).Status);
        }
    }

    [Fact]
    public async Task Delete_soft_deletes_room_and_keeps_tokens()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var roomId = await CreateRoomAsync(provider);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<Guid>>();
            Assert.True((await uc.ExecuteAsync(roomId)).IsSuccess);

            var missing = await uc.ExecuteAsync(Guid.CreateVersion7());
            Assert.True(missing.IsFailure);
            Assert.Equal(RoomsErrors.RoomNotFound.Code, missing.Error.Code);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            // Query filter loại phòng xóa mềm → không còn thấy qua DbSet mặc định.
            Assert.False(await db.Rooms.AnyAsync(r => r.Id == roomId));
            // Nhưng token con vẫn giữ (không cascade xóa cứng — lịch sử).
            Assert.True(await db.RoomQrTokens.AnyAsync(t => t.RoomId == roomId));
        }
    }
}
