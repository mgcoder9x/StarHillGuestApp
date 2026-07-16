using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// F.1c — <see cref="IdentityUserSeeder"/> IDEMPOTENT (Postgres/migration + Argon2 base). Chạy 2 lần → đúng MỘT admin
/// (không nhân đôi), password đã băm (KHÔNG thô), role Admin + IsActive. SKIP nếu thiếu Docker. Cùng collection serialize.
/// </summary>
[Collection(IdentityIntegrationDefinition.Name)]
public sealed class IdentityUserSeederTests : IAsyncLifetime
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
#pragma warning disable CA1031
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

    private static IConfiguration JwtConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ActiveKid"] = "k1",
            ["Jwt:Issuer"] = "t",
            ["Jwt:Audience"] = "t",
            ["Jwt:Keys:0:Kid"] = "k1",
            ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
        }).Build();

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock>(new FixedClock());
        services.AddBedrockSecurity(JwtConfig());
        services.AddIdentityInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [SkippableFact]
    public async Task Seed_admin_is_idempotent()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Database.MigrateAsync();
        }

        async Task<bool> Seed()
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IdentityUserSeeder>();
            return await seeder.SeedAdminAsync("admin", "DevAdmin!2026");
        }

        var first = await Seed();
        var second = await Seed();

        Assert.True(first);   // lần đầu tạo
        Assert.False(second); // lần hai idempotent — không tạo lại

        await using var readScope = provider.CreateAsyncScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var count = await readDb.Users.CountAsync(u => u.Username == "admin");
        Assert.Equal(1, count);
        var admin = await readDb.Users.AsNoTracking().SingleAsync(u => u.Username == "admin");
        Assert.Equal(Identity.Domain.UserRole.Admin, admin.Role);
        Assert.True(admin.IsActive);
        Assert.NotEqual("DevAdmin!2026", admin.PasswordHash); // đã băm, không thô
    }
}
