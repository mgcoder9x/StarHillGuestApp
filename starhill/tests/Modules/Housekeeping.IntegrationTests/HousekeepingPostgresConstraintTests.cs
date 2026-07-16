using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Housekeeping.Domain;
using Housekeeping.Infrastructure.DependencyInjection;
using Housekeeping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Housekeeping.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc DB module Housekeeping — áp migration THẬT (đường production).
/// SKIP nếu thiếu Docker (N-067). Phủ: đúng MỘT ticket MỞ/phòng (partial unique <c>ux_hk_open_ticket_room</c> filter
/// <c>status IN ('Requested','InProgress')</c> — Req 6.2) + slot mở giải phóng sau Done/Cancelled. H-Hk.1 = persistence nền.
/// </summary>
public sealed class HousekeepingPostgresConstraintTests : IAsyncLifetime
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddHousekeepingInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "housekeeping")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
        await db.Database.MigrateAsync();
    }

    private static HousekeepingTicket NewTicket(Guid resortId, Guid roomId, HousekeepingStatus status) => new()
    {
        ResortId = resortId,
        RoomId = roomId,
        Status = status,
        CreatedAt = DateTimeOffset.UnixEpoch,
    };

    [SkippableFact]
    public async Task Only_one_open_ticket_per_room_but_reusable_after_done()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        // (1) Hai ticket MỞ cùng phòng → vi phạm ux_hk_open_ticket_room.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            db.HousekeepingTickets.Add(NewTicket(resortId, roomId, HousekeepingStatus.Requested));
            await db.SaveChangesAsync();

            db.HousekeepingTickets.Add(NewTicket(resortId, roomId, HousekeepingStatus.InProgress));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // (2) Đóng ticket mở (Done) → tạo ticket mở mới CHO PHÉP (slot mở giải phóng — filter loại Done).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            var open = await db.HousekeepingTickets.FirstAsync(t => t.RoomId == roomId && t.Status == HousekeepingStatus.Requested);
            open.Status = HousekeepingStatus.Done;
            open.CompletedAt = DateTimeOffset.UnixEpoch;
            await db.SaveChangesAsync();

            db.HousekeepingTickets.Add(NewTicket(resortId, roomId, HousekeepingStatus.Requested));
            await db.SaveChangesAsync(); // không ném — slot mở đã giải phóng.
        }
    }

    [SkippableFact]
    public async Task Cancelled_ticket_frees_open_slot()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();

        var first = NewTicket(resortId, roomId, HousekeepingStatus.Requested);
        db.HousekeepingTickets.Add(first);
        await db.SaveChangesAsync();

        // Huỷ (cascade visit-end) → Cancelled không nằm trong filter → tạo mới cho phép.
        first.Status = HousekeepingStatus.Cancelled;
        await db.SaveChangesAsync();

        db.HousekeepingTickets.Add(NewTicket(resortId, roomId, HousekeepingStatus.Requested));
        await db.SaveChangesAsync();
    }
}
