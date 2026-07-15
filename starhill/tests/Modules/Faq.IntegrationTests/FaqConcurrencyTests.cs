using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Faq.Domain;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Faq.IntegrationTests;

/// <summary>
/// CP15 (optimistic concurrency) — hai lễ tân sửa CÙNG một <see cref="FaqItem"/> đồng thời: người ghi sau nhận xung
/// đột concurrency (KHÔNG ghi đè âm thầm). Chứng minh token <c>xmin</c> (map bởi <c>PlatformDbContext</c> cho entity
/// <c>IHasConcurrencyToken</c> trên Npgsql) đang HOẠT ĐỘNG trên <see cref="FaqItem"/> — mirror RuleConcurrencyTests.
/// BUỘC PostgreSQL thật (SQLite không có xmin) → Testcontainers; SKIP nếu thiếu Docker (N-067).
/// </summary>
public sealed class FaqConcurrencyTests : IAsyncLifetime
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
        services.AddFaqInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "faq")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
        await db.Database.MigrateAsync();
    }

    [SkippableFact]
    public async Task Concurrent_item_update_second_writer_gets_concurrency_conflict()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        // Seed category + item.
        Guid itemId;
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var category = new FaqCategory { ResortId = resortId, Key = "arrival", SortOrder = 1, IsActive = true };
            db.FaqCategories.Add(category);
            var item = new FaqItem { ResortId = resortId, CategoryId = category.Id, SortOrder = 1, IsActive = true };
            db.FaqItems.Add(item);
            await db.SaveChangesAsync();
            itemId = item.Id;
        }

        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<FaqDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<FaqDbContext>();

        var itemA = await dbA.FaqItems.SingleAsync(i => i.Id == itemId);
        var itemB = await dbB.FaqItems.SingleAsync(i => i.Id == itemId);

        itemA.SortOrder = 5;
        await dbA.SaveChangesAsync(); // Người ghi trước — thành công, xmin đổi.

        itemB.SortOrder = 9;
        // Người ghi sau — xmin cũ không còn khớp → concurrency conflict (không ghi đè âm thầm).
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}
