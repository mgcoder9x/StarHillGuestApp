using System;
using System.Threading.Tasks;
using ResortQr.Domain.GuestAccess;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Harness SQLite in-memory (Docker-free) cho <see cref="AppDbContext"/> — DB quan hệ THẬT (enforce
/// unique/partial-unique/FK). Bật <c>Foreign Keys=True</c> để kiểm FK Restrict. KHÔNG kiểm xmin/race Postgres.
/// </summary>
public sealed class AppSqliteHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppSqliteHarness(DateTimeOffset now, Guid? actor = null)
    {
        Clock = new MutableClock(now);
        CurrentUser = new FixedCurrentUser(actor);

        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    public MutableClock Clock { get; }

    public FixedCurrentUser CurrentUser { get; }

    /// <summary>Context MỚI mỗi lần (scope riêng) nhưng CÙNG DB (chung connection).</summary>
    public AppDbContext CreateContext() => new(_options, Clock, CurrentUser);

    /// <summary>Seed một resort và trả về Id (thoả FK cho các entity phụ thuộc).</summary>
    public async Task<Guid> SeedResortAsync()
    {
        await using var ctx = CreateContext();
        var resort = new Resort { Name = "Test Resort", Timezone = "Asia/Ho_Chi_Minh", CreatedAt = Clock.UtcNow };
        ctx.Resorts.Add(resort);
        await ctx.SaveChangesAsync();
        return resort.Id;
    }

    public async Task<Guid> SeedRoomAsync(Guid resortId, string roomNumber = "101")
    {
        await using var ctx = CreateContext();
        var room = new Room { ResortId = resortId, RoomNumber = roomNumber };
        ctx.Rooms.Add(room);
        await ctx.SaveChangesAsync();
        return room.Id;
    }

    public async Task<Guid> SeedSessionAsync(string keyHash = "hash-1")
    {
        await using var ctx = CreateContext();
        var session = new GuestSession
        {
            SessionKeyHash = keyHash,
            FirstSeenAt = Clock.UtcNow,
            LastSeenAt = Clock.UtcNow,
        };
        ctx.GuestSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    public void Dispose() => _connection.Dispose();
}
