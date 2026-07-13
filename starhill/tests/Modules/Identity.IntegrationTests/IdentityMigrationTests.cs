using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Users;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL — vận hành hoá, §4.6): kiểm chứng EF <b>migration per-module</b> THAY
/// <c>EnsureCreated</c> dev-only. Chạy <c>Database.MigrateAsync()</c> trên Postgres SẠCH → schema <c>identity</c>
/// tạo đúng (outbox/inbox/refresh_token + __EFMigrationsHistory trong schema module), và round-trip store hoạt
/// động → migration KHỚP model runtime (không drift). Skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(IdentityIntegrationDefinition.Name)]
public sealed class IdentityMigrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (không fail). Root-cause N-067.
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: lỗi khởi động container ⇒ coi như thiếu Docker → skip.
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

    [SkippableFact]
    public async Task Migrate_creates_identity_schema_and_store_round_trips()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua migration integration test.");

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddIdentityInfrastructure(o => o.UseNpgsql(_container.GetConnectionString()));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // ÁP MIGRATION (không EnsureCreated) — đúng đường production.
        await db.Database.MigrateAsync();

        // (1) Migration được ghi nhận trong __EFMigrationsHistory (schema identity → migrate per-module).
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, m => m.Contains("InitialCreate", StringComparison.Ordinal));

        // (2) Bảng outbox_message tồn tại + query được (schema khớp model): không ném là đủ chứng minh bảng/cột đúng.
        Assert.False(await db.Set<OutboxMessage>().AnyAsync());

        // (3) Round-trip refresh_token qua store (ux_refresh_hash + cột snake_case hoạt động thật).
        // KEYED (P0-1): port persistence Identity đăng ký theo module key → resolve keyed đúng module.
        var store = scope.ServiceProvider.GetRequiredKeyedService<IRefreshTokenStore>(IdentityInfrastructureExtensions.PersistenceKey);
        var uow = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(IdentityInfrastructureExtensions.PersistenceKey);
        var snapshot = new RefreshTokenSnapshot(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            "hash-migrate", DateTimeOffset.UtcNow.AddDays(30), null);
        await store.AddAsync(snapshot);
        await uow.SaveChangesAsync();

        var found = await store.GetByHashAsync("hash-migrate");
        Assert.NotNull(found);
        Assert.Equal(snapshot.Id, found.Id);
    }
}
