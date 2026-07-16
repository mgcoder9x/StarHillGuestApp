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
/// CP15 (optimistic concurrency) — hai nhân viên cùng đổi trạng thái MỘT ticket đồng thời: người ghi sau nhận xung
/// đột (KHÔNG ghi đè âm thầm). Chứng minh <c>xmin</c> (map bởi PlatformDbContext cho <c>IHasConcurrencyToken</c> trên
/// Npgsql) HOẠT ĐỘNG trên <see cref="HousekeepingTicket"/>. BUỘC PostgreSQL (SQLite không xmin) → Testcontainers; SKIP nếu thiếu Docker.
/// </summary>
public sealed class HousekeepingConcurrencyTests : IAsyncLifetime
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

    [SkippableFact]
    public async Task Concurrent_ticket_update_second_writer_gets_concurrency_conflict()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        Guid ticketId;
        await using (var seed = provider.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            var ticket = new HousekeepingTicket
            {
                ResortId = Guid.CreateVersion7(),
                RoomId = Guid.CreateVersion7(),
                Status = HousekeepingStatus.Requested,
                CreatedAt = DateTimeOffset.UnixEpoch,
            };
            db.HousekeepingTickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<HousekeepingDbContext>();

        var a = await dbA.HousekeepingTickets.SingleAsync(t => t.Id == ticketId);
        var b = await dbB.HousekeepingTickets.SingleAsync(t => t.Id == ticketId);

        a.Status = HousekeepingStatus.InProgress;
        await dbA.SaveChangesAsync(); // trước — thành công, xmin đổi.

        b.Status = HousekeepingStatus.Done;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}
