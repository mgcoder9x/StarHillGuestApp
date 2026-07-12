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
/// Kiểm <see cref="IResortSettingsQuery"/> (EF impl) trên SQLite in-memory (DB quan hệ thật, KHÔNG cần Docker →
/// CHẠY CỤC BỘ). Query chỉ đọc (không dùng partial-index/xmin Postgres-specific) nên SQLite phản ánh đúng hành vi
/// map entity→snapshot + single-resort + null-khi-chưa-seed. Consumer thật: Rooms.RenderQrPng đọc GuestWebBaseUrl.
/// </summary>
public sealed class ResortSettingsQueryTests
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
    public async Task GetAsync_returns_null_when_not_seeded()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IResortSettingsQuery>();

        Assert.Null(await query.GetAsync());
    }

    [Fact]
    public async Task GetAsync_returns_snapshot_with_seeded_defaults()
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
            var query = scope.ServiceProvider.GetRequiredService<IResortSettingsQuery>();
            var snapshot = await query.GetAsync();

            Assert.NotNull(snapshot);
            Assert.NotEqual(Guid.Empty, snapshot.ResortId);
            // Mặc định seed (Req 14): feature flags bật, ack tắt, cửa sổ 30', idle 24h, base URL chưa cấu hình.
            Assert.True(snapshot.FaqEnabled);
            Assert.True(snapshot.ChatEnabled);
            Assert.True(snapshot.HousekeepingEnabled);
            Assert.False(snapshot.RequireRuleAckForFaq);
            Assert.Equal(30, snapshot.PortalWindowMinutes);
            Assert.Equal(24, snapshot.VisitIdleExpiryHours);
            Assert.Null(snapshot.GuestWebBaseUrl);
            Assert.Equal(2000, snapshot.MaxMessageLength);
        }
    }
}
