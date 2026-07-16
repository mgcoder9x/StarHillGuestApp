using Bedrock.Application.Ports.Users;
using Identity.Domain;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc bảng <c>users</c> (F.1a — QR-AD-038): áp migration THẬT rồi
/// chứng minh (1) round-trip user (ghi/đọc, cột snake_case + Role enum→string hoạt động); (2) unique
/// <c>ux_identity_user_username</c> (hai user cùng username → vi phạm). Skip nếu thiếu Docker (N-067). Cùng
/// collection serialize (một Postgres một lúc).
/// </summary>
[Collection(IdentityIntegrationDefinition.Name)]
public sealed class IdentityPostgresConstraintTests : IAsyncLifetime
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

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddIdentityInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static IdentityUser NewUser(string username) => new()
    {
        Username = username,
        PasswordHash = "$argon2id$v=19$m=1,t=1,p=1$c2FsdA$aGFzaA",
        Role = UserRole.Admin,
        IsActive = true,
        DisplayName = "Admin",
        CreatedAt = DateTimeOffset.UnixEpoch,
    };

    [SkippableFact]
    public async Task User_round_trips_and_username_is_unique()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Database.MigrateAsync();
        }

        // (1) Round-trip: ghi user + đọc lại (Role enum→string, IsActive, snake_case cột).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Users.Add(NewUser("admin"));
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var loaded = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "admin");
            Assert.Equal(UserRole.Admin, loaded.Role);
            Assert.True(loaded.IsActive);
            Assert.Equal("Admin", loaded.DisplayName);
        }

        // (2) Username unique: user thứ hai cùng username → vi phạm ux_identity_user_username.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.Users.Add(NewUser("admin"));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
