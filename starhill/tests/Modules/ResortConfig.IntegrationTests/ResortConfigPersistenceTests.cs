using Bedrock.Application.Ports.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Domain;
using ResortConfig.Infrastructure.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace ResortConfig.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL — vận hành hoá). Kiểm chứng ràng buộc DB THẬT của module ResortConfig
/// (không mô phỏng được trên SQLite/InMemory): CP14 (đúng một ngôn ngữ mặc định — partial unique index),
/// CP15 (chống ghi đè đồng thời ResortSettings — xmin concurrency), và seeder idempotent (Req 12.4).
/// Một container dùng chung cho cả lớp; SKIP nếu thiếu Docker (N-067 — Build() nằm trong try để catch → skip).
/// </summary>
public sealed class ResortConfigPersistenceTests : IAsyncLifetime
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

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddResortConfigInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "resort_config")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task MigrateAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
        await db.Database.MigrateAsync();
    }

    // ─────────────────────────── CP14 ───────────────────────────
    [SkippableFact]
    public async Task Partial_unique_index_allows_only_one_default_language_per_resort()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = BuildProvider();
        await MigrateAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();

        var resort = new Resort { Name = "R", Timezone = "Asia/Ho_Chi_Minh", CreatedAt = DateTimeOffset.UtcNow };
        db.Resorts.Add(resort);
        db.ResortLanguages.Add(new ResortLanguage { ResortId = resort.Id, Code = "en", DisplayName = "English", IsDefault = true });
        await db.SaveChangesAsync();

        // Thêm ngôn ngữ mặc định THỨ HAI cho cùng resort → vi phạm partial unique ux_lang_default.
        db.ResortLanguages.Add(new ResortLanguage { ResortId = resort.Id, Code = "vi", DisplayName = "Tiếng Việt", IsDefault = true });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    // ─────────────────────────── CP15 ───────────────────────────
    [SkippableFact]
    public async Task Concurrent_settings_update_second_writer_gets_concurrency_conflict()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = BuildProvider();
        await MigrateAsync(provider);

        Guid resortId;
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
            var resort = new Resort { Name = "R", Timezone = "Asia/Ho_Chi_Minh", CreatedAt = DateTimeOffset.UtcNow };
            db.Resorts.Add(resort);
            db.ResortSettingsSet.Add(new ResortSettings { ResortId = resort.Id });
            await db.SaveChangesAsync();
            resortId = resort.Id;
        }

        // Hai scope/DbContext độc lập đọc cùng bản ghi → cùng snapshot xmin.
        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<ResortConfigDbContext>();

        var settingsA = await dbA.ResortSettingsSet.SingleAsync(s => s.ResortId == resortId);
        var settingsB = await dbB.ResortSettingsSet.SingleAsync(s => s.ResortId == resortId);

        settingsA.PortalWindowMinutes = 45;
        await dbA.SaveChangesAsync(); // Người ghi trước — thành công, xmin đổi.

        settingsB.PortalWindowMinutes = 50;
        // Người ghi sau — xmin cũ không còn khớp → concurrency conflict (không ghi đè âm thầm).
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    // ─────────────────────── Seeder idempotent (Req 12.4) ───────────────────────
    [SkippableFact]
    public async Task Seeder_is_idempotent_and_sets_english_as_single_default()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = BuildProvider();
        await MigrateAsync(provider);

        // Chạy seeder HAI lần → không nhân đôi.
        for (var i = 0; i < 2; i++)
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ResortConfigSeeder>();
            await seeder.SeedAsync();
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();

        Assert.Equal(1, await db.Resorts.CountAsync());
        Assert.Equal(1, await db.ResortSettingsSet.CountAsync());
        Assert.Equal(4, await db.ResortLanguages.CountAsync());

        var defaults = await db.ResortLanguages.Where(l => l.IsDefault).ToListAsync();
        Assert.Single(defaults);
        Assert.Equal("en", defaults[0].Code);
    }
}
