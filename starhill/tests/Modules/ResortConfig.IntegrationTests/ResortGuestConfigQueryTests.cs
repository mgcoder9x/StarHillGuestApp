using Bedrock.Application.Ports.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using ResortConfig.Infrastructure.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Xunit;

namespace ResortConfig.IntegrationTests;

/// <summary>
/// Kiểm <see cref="IResortGuestConfigQuery"/> (EF impl) trên SQLite in-memory (CHẠY CỤC BỘ, không Docker). Query
/// chỉ đọc/join → SQLite phản ánh đúng: gộp resort+settings+languages; đúng một mặc định; FAIL-CLOSED khi resort
/// không tồn tại hoặc resortId sai (QR-AD-024). Consumer thật: GuestAccess.resolve.
/// </summary>
public sealed class ResortGuestConfigQueryTests
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
        services.AddResortConfigInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    [Fact]
    public async Task GetAsync_returns_null_when_resort_not_seeded()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IResortGuestConfigQuery>();

        Assert.Null(await query.GetAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task GetAsync_returns_config_for_seeded_resort()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        Guid resortId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<ResortConfigSeeder>();
            await seeder.SeedAsync();
            var db = scope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
            resortId = (await db.Resorts.AsNoTracking().FirstAsync()).Id;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<IResortGuestConfigQuery>();
            var config = await query.GetAsync(resortId);

            Assert.NotNull(config);
            Assert.Equal(resortId, config.ResortId);
            Assert.False(string.IsNullOrWhiteSpace(config.ResortName));
            // Seed Req 12.4: en/vi/ko/zh enabled, 'en' mặc định.
            Assert.Equal("en", config.DefaultLanguageCode);
            Assert.Contains("en", config.EnabledLanguageCodes);
            Assert.Contains("vi", config.EnabledLanguageCodes);
            Assert.Contains(config.DefaultLanguageCode, config.EnabledLanguageCodes);
            // Cấu hình vận hành mặc định (Req 14).
            Assert.True(config.FaqEnabled);
            Assert.True(config.ChatEnabled);
            Assert.True(config.HousekeepingEnabled);
            Assert.Equal(30, config.PortalWindowMinutes);
            Assert.Equal(24, config.VisitIdleExpiryHours);
        }
    }

    [Fact]
    public async Task GetAsync_returns_null_for_mismatched_resort_id()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using (var scope = provider.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<ResortConfigSeeder>();
            await seeder.SeedAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<IResortGuestConfigQuery>();
            // resortId khác resort đã seed → FAIL-CLOSED (không "khớp ngầm" resort đơn).
            Assert.Null(await query.GetAsync(Guid.CreateVersion7()));
        }
    }
}
