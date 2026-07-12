using Bedrock.Application.Ports.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rooms.Contracts;
using Rooms.Domain;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace Rooms.IntegrationTests;

/// <summary>
/// Kiểm persistence module Rooms trên SQLite in-memory (DB quan hệ thật, CHẠY CỤC BỘ — không cần Docker).
/// Bao phủ: CP2 (partial unique <c>ux_qr_active</c> — 1 token Active/phòng, provider-agnostic filter status='Active'),
/// token unique toàn cục (<c>ux_qrtoken_token</c>), và <see cref="IRoomTokenResolver"/> (CP1: token Active→phòng;
/// token lạ/revoked/phòng xóa mềm → null; phòng Inactive → IsRoomActive=false). Luật <c>ux_room_number</c> (partial
/// Npgsql-only) test riêng trên Postgres (Testcontainers) — xem <see cref="RoomsPostgresConstraintTests"/>.
/// </summary>
public sealed class RoomsPersistenceTests
{
    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddRoomsInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static Room NewRoom(RoomStatus status = RoomStatus.Active) => new()
    {
        ResortId = Guid.CreateVersion7(),
        RoomNumber = "A-203",
        Building = "A",
        Floor = 2,
        Status = status,
    };

    private static RoomQrToken ActiveToken(Guid roomId, string token) => new()
    {
        RoomId = roomId,
        Token = token,
        TokenPreview = token[..Math.Min(6, token.Length)],
        Status = RoomQrTokenStatus.Active,
        Version = 1,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    // ─────────────────────── CP2 ───────────────────────
    [Fact]
    public async Task Only_one_active_token_per_room_is_allowed()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();

        var room = NewRoom();
        db.Rooms.Add(room);
        db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-1"));
        await db.SaveChangesAsync();

        // Token Active THỨ HAI cho cùng phòng → vi phạm partial unique ux_qr_active.
        db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-2"));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Revoked_token_frees_the_active_slot()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();

        var room = NewRoom();
        db.Rooms.Add(room);
        var first = ActiveToken(room.Id, "tok-old");
        db.RoomQrTokens.Add(first);
        await db.SaveChangesAsync();

        // Revoke token cũ + cấp token Active mới (mô phỏng rotate) → KHÔNG vi phạm (chỉ 1 Active).
        first.Status = RoomQrTokenStatus.Revoked;
        first.RevokedAt = DateTimeOffset.UtcNow;
        db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-new"));
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.RoomQrTokens.CountAsync(t => t.RoomId == room.Id && t.Status == RoomQrTokenStatus.Active));
        Assert.Equal(2, await db.RoomQrTokens.CountAsync(t => t.RoomId == room.Id)); // giữ lịch sử.
    }

    [Fact]
    public async Task Global_token_uniqueness_is_enforced()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();

        var roomA = NewRoom();
        var roomB = NewRoom();
        db.Rooms.AddRange(roomA, roomB);
        db.RoomQrTokens.Add(ActiveToken(roomA.Id, "dup"));
        await db.SaveChangesAsync();

        db.RoomQrTokens.Add(ActiveToken(roomB.Id, "dup")); // cùng token string toàn cục → ux_qrtoken_token.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // ─────────────────────── CP1 — resolver ───────────────────────
    [Fact]
    public async Task Resolver_returns_room_for_active_token()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        Guid roomId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = NewRoom();
            db.Rooms.Add(room);
            db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-ok"));
            await db.SaveChangesAsync();
            roomId = room.Id;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IRoomTokenResolver>();
            var res = await resolver.ResolveActiveTokenAsync("tok-ok");
            Assert.NotNull(res);
            Assert.Equal(roomId, res.RoomId);
            Assert.Equal("A-203", res.RoomNumber);
            Assert.True(res.IsRoomActive);

            Assert.Null(await resolver.ResolveActiveTokenAsync("khong-ton-tai"));
        }
    }

    [Fact]
    public async Task Resolver_returns_null_for_revoked_token()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = NewRoom();
            db.Rooms.Add(room);
            db.RoomQrTokens.Add(new RoomQrToken
            {
                RoomId = room.Id,
                Token = "tok-revoked",
                TokenPreview = "tok-re",
                Status = RoomQrTokenStatus.Revoked,
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                RevokedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IRoomTokenResolver>();
            Assert.Null(await resolver.ResolveActiveTokenAsync("tok-revoked"));
        }
    }

    [Fact]
    public async Task Resolver_returns_null_when_room_soft_deleted()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        Guid roomId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = NewRoom();
            db.Rooms.Add(room);
            db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-del"));
            await db.SaveChangesAsync();
            roomId = room.Id;
        }

        // Xóa mềm trong scope RIÊNG chỉ load room (đúng pattern DeleteRoomUseCase — KHÔNG track token con,
        // tránh EF cascade child khi Room chuyển Deleted trước khi interceptor đổi thành soft-delete).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = await db.Rooms.FirstAsync(r => r.Id == roomId);
            db.Rooms.Remove(room); // ISoftDeletable → interceptor chuyển thành xóa mềm ở SaveChanges.
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IRoomTokenResolver>();
            Assert.Null(await resolver.ResolveActiveTokenAsync("tok-del"));
        }
    }

    [Fact]
    public async Task Resolver_flags_inactive_room()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var room = NewRoom(RoomStatus.Inactive);
            db.Rooms.Add(room);
            db.RoomQrTokens.Add(ActiveToken(room.Id, "tok-inactive"));
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<IRoomTokenResolver>();
            var res = await resolver.ResolveActiveTokenAsync("tok-inactive");
            Assert.NotNull(res);
            Assert.False(res.IsRoomActive); // phòng tồn tại nhưng Inactive → consumer map room_inactive.
        }
    }
}
