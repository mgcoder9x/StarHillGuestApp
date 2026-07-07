using System;
using System.Linq;
using System.Threading.Tasks;
using ResortQr.Domain.GuestAccess;
using ResortQr.Domain.Identity;
using ResortQr.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Kiểm mapping/ràng buộc AppDbContext trên SQLite THẬT (Docker-free): enum→string, unique, partial-unique,
/// FK Restrict, soft-delete + audit. (xmin/race Postgres cần Testcontainers — hoãn.)
/// </summary>
public sealed class AppPersistenceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enum_status_persists_as_string_and_roundtrips()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId);
        var sessionId = await harness.SeedSessionAsync();

        await using (var ctx = harness.CreateContext())
        {
            ctx.GuestVisits.Add(new GuestVisit
            {
                ResortId = resortId,
                RoomId = roomId,
                GuestSessionId = sessionId,
                Status = GuestVisitStatus.Expired,
                StartedAt = T0,
                LastSeenAt = T0,
                ExpiresAt = T0.AddHours(24),
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var visit = await ctx.GuestVisits.SingleAsync();
            Assert.Equal(GuestVisitStatus.Expired, visit.Status);

            var raw = await ctx.Database
                .SqlQueryRaw<string>("SELECT status AS \"Value\" FROM guest_visit")
                .SingleAsync();
            Assert.Equal("Expired", raw);
        }
    }

    [Fact]
    public async Task AppUser_duplicate_email_is_blocked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();

        await using var ctx = harness.CreateContext();
        ctx.AppUsers.Add(NewUser(resortId, "a@x.com"));
        ctx.AppUsers.Add(NewUser(resortId, "a@x.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task RoomQrToken_duplicate_token_is_blocked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var room1 = await harness.SeedRoomAsync(resortId, "101");
        var room2 = await harness.SeedRoomAsync(resortId, "102");

        await using var ctx = harness.CreateContext();
        ctx.RoomQrTokens.Add(NewToken(room1, "dup-token"));
        ctx.RoomQrTokens.Add(NewToken(room2, "dup-token"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task RoomQrToken_two_active_same_room_is_blocked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId);

        await using (var ctx = harness.CreateContext())
        {
            ctx.RoomQrTokens.Add(NewToken(roomId, "t1"));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            ctx.RoomQrTokens.Add(NewToken(roomId, "t2"));
            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task RoomQrToken_revoke_then_new_active_is_allowed()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId);

        Guid firstId;
        await using (var ctx = harness.CreateContext())
        {
            var t1 = NewToken(roomId, "t1");
            ctx.RoomQrTokens.Add(t1);
            await ctx.SaveChangesAsync();
            firstId = t1.Id;
        }

        await using (var ctx = harness.CreateContext())
        {
            var t1 = await ctx.RoomQrTokens.SingleAsync(t => t.Id == firstId);
            t1.Status = RoomQrTokenStatus.Revoked;
            t1.RevokedAt = T0;
            ctx.RoomQrTokens.Add(NewToken(roomId, "t2"));
            await ctx.SaveChangesAsync(); // không ném: chỉ 1 Active/phòng
        }

        await using (var verify = harness.CreateContext())
        {
            var activeCount = await verify.RoomQrTokens
                .CountAsync(t => t.RoomId == roomId && t.Status == RoomQrTokenStatus.Active);
            Assert.Equal(1, activeCount);
        }
    }

    [Fact]
    public async Task GuestVisit_two_active_same_session_room_is_blocked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId);
        var sessionId = await harness.SeedSessionAsync();

        await using var ctx = harness.CreateContext();
        ctx.GuestVisits.Add(NewVisit(resortId, roomId, sessionId));
        ctx.GuestVisits.Add(NewVisit(resortId, roomId, sessionId));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Room_number_duplicate_when_not_deleted_is_blocked()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        await harness.SeedRoomAsync(resortId, "201");

        await using var ctx = harness.CreateContext();
        ctx.Rooms.Add(new Room { ResortId = resortId, RoomNumber = "201" });

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Room_number_can_be_reused_after_soft_delete()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId, "301");

        await using (var ctx = harness.CreateContext())
        {
            var room = await ctx.Rooms.SingleAsync(r => r.Id == roomId);
            ctx.Rooms.Remove(room); // soft-delete (base chuyển Delete→Modified)
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            ctx.Rooms.Add(new Room { ResortId = resortId, RoomNumber = "301" });
            await ctx.SaveChangesAsync(); // OK: bản cũ đã is_deleted
        }
    }

    [Fact]
    public async Task Room_soft_delete_sets_flags_and_is_filtered_from_queries()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId, "401");

        await using (var ctx = harness.CreateContext())
        {
            ctx.Rooms.Remove(await ctx.Rooms.SingleAsync(r => r.Id == roomId));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            Assert.False(await ctx.Rooms.AnyAsync(r => r.Id == roomId));

            var deleted = await ctx.Rooms.IgnoreQueryFilters().SingleAsync(r => r.Id == roomId);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
        }
    }

    [Fact]
    public async Task Room_audit_sets_created_then_updated_without_touching_created()
    {
        using var harness = new AppSqliteHarness(T0, actor: Guid.CreateVersion7());
        var resortId = await harness.SeedResortAsync();
        var roomId = await harness.SeedRoomAsync(resortId, "501");

        harness.Clock.UtcNow = T0.AddHours(5);
        await using (var ctx = harness.CreateContext())
        {
            var room = await ctx.Rooms.SingleAsync(r => r.Id == roomId);
            room.Status = RoomStatus.Maintenance;
            await ctx.SaveChangesAsync();
        }

        await using (var verify = harness.CreateContext())
        {
            var room = await verify.Rooms.SingleAsync(r => r.Id == roomId);
            Assert.Equal(T0, room.CreatedAt);
            Assert.Equal(T0.AddHours(5), room.UpdatedAt);
        }
    }

    [Fact]
    public async Task Room_with_nonexistent_resort_violates_fk_restrict()
    {
        using var harness = new AppSqliteHarness(T0);

        await using var ctx = harness.CreateContext();
        ctx.Rooms.Add(new Room { ResortId = Guid.CreateVersion7(), RoomNumber = "601" });

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    private static AppUser NewUser(Guid resortId, string email) => new()
    {
        ResortId = resortId,
        Email = email,
        DisplayName = "User",
        PasswordHash = "hash",
        Role = UserRole.Staff,
    };

    private static RoomQrToken NewToken(Guid roomId, string token) => new()
    {
        RoomId = roomId,
        Token = token,
        TokenPreview = token.Length <= 4 ? token : token[^4..],
        Status = RoomQrTokenStatus.Active,
        CreatedAt = T0,
    };

    private static GuestVisit NewVisit(Guid resortId, Guid roomId, Guid sessionId) => new()
    {
        ResortId = resortId,
        RoomId = roomId,
        GuestSessionId = sessionId,
        Status = GuestVisitStatus.Active,
        StartedAt = T0,
        LastSeenAt = T0,
        ExpiresAt = T0.AddHours(24),
    };
}
