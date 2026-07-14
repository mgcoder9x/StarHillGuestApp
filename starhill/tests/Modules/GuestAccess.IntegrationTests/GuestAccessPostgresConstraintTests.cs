using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using GuestAccess.Domain;
using GuestAccess.Infrastructure.DependencyInjection;
using GuestAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace GuestAccess.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc DB của GuestAccess — áp migration THẬT (đường production)
/// để chứng minh index/constraint có trong migration, không chỉ trong model. SKIP nếu thiếu Docker (N-067).
/// Phủ: unique hash session; partial unique 1 Active/(session,room) + giải phóng sau khi đóng; check vòng đời.
/// </summary>
public sealed class GuestAccessPostgresConstraintTests : IAsyncLifetime
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
        services.AddGuestAccessInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "guest_access")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static GuestSession NewSession(string hash) => new()
    {
        SessionKeyHash = hash,
        FirstSeenAt = DateTimeOffset.UnixEpoch,
        LastSeenAt = DateTimeOffset.UnixEpoch,
    };

    private static GuestVisit NewVisit(Guid sessionId, Guid roomId, DateTimeOffset now) => new()
    {
        ResortId = Guid.CreateVersion7(),
        RoomId = roomId,
        GuestSessionId = sessionId,
        Status = GuestVisitStatus.Active,
        StartedAt = now,
        LastSeenAt = now,
        ExpiresAt = now.AddHours(24),
    };

    [SkippableFact]
    public async Task Session_key_hash_is_globally_unique()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        db.GuestSessions.Add(NewSession("hash-dup"));
        await db.SaveChangesAsync();

        db.GuestSessions.Add(NewSession("hash-dup"));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Only_one_active_visit_per_session_room_but_reusable_after_close()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var roomId = Guid.CreateVersion7();

        // (1) Hai visit Active cùng (session, room) → vi phạm ux_guest_visit_active.
        Guid sessionId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
            var session = NewSession("hash-visit");
            db.GuestSessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;

            db.GuestVisits.Add(NewVisit(sessionId, roomId, now));
            await db.SaveChangesAsync();

            db.GuestVisits.Add(NewVisit(sessionId, roomId, now));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // (2) Đóng visit Active hiện tại rồi tạo visit Active mới cùng (session, room) → CHO PHÉP (partial filter).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
            var active = await db.GuestVisits.FirstAsync(v => v.Status == GuestVisitStatus.Active);
            active.Status = GuestVisitStatus.Closed;
            active.ClosedAt = now.AddMinutes(5);
            await db.SaveChangesAsync();

            db.GuestVisits.Add(NewVisit(sessionId, roomId, now.AddMinutes(10)));
            await db.SaveChangesAsync(); // không ném — slot Active đã giải phóng.
        }
    }

    [SkippableFact]
    public async Task Check_constraint_rejects_expiry_before_last_seen()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        var session = NewSession("hash-check");
        db.GuestSessions.Add(session);
        await db.SaveChangesAsync();

        var bad = NewVisit(session.Id, Guid.CreateVersion7(), now);
        bad.ExpiresAt = now.AddHours(-1); // vi phạm ck_guest_visit_expiry_after_seen.
        db.GuestVisits.Add(bad);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Check_constraint_rejects_active_visit_with_closed_at()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        var session = NewSession("hash-check2");
        db.GuestSessions.Add(session);
        await db.SaveChangesAsync();

        var bad = NewVisit(session.Id, Guid.CreateVersion7(), now);
        bad.Status = GuestVisitStatus.Active;
        bad.ClosedAt = now; // Active mà đã có closed_at → vi phạm ck_guest_visit_closed_at.
        db.GuestVisits.Add(bad);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GuestAccessDbContext>();
        await db.Database.MigrateAsync();
    }
}
