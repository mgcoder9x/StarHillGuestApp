using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Entities;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

// ── Entity + context riêng cho test compose (A-25) ──────────────────────────
// TenantScopedThing chỉ soft-deletable (KHÔNG IAuditable) → cô lập hành vi filter, không dính audit.
public sealed class TenantScopedThing : Entity, ISoftDeletable
{
    public string Tenant { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// DbContext dẫn xuất đăng KÝ THÊM một named query filter riêng ("Tenant") ngoài filter "SoftDelete" mà
/// <see cref="PlatformDbContext.OnModelCreating"/> đã cài. Mô phỏng một module bổ sung filter tenant.
/// EF Core 10 COMPOSE (AND) các named filter → cả hai cùng áp; nếu base dùng UNNAMED filter thì filter
/// thứ hai sẽ GHI ĐÈ và soft-delete mất hiệu lực — chính là hồi quy A-25 phòng ngừa.
/// </summary>
public sealed class ComposeFilterDbContext(
    DbContextOptions<ComposeFilterDbContext> options,
    IClock clock,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher)
    : PlatformDbContext(options, clock, currentUser, dispatcher)
{
    public const string TenantFilterName = "Tenant";
    public const string VisibleTenant = "acme";

    public DbSet<TenantScopedThing> Items => Set<TenantScopedThing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // cài filter "SoftDelete" cho ISoftDeletable
        modelBuilder.Entity<TenantScopedThing>()
            .HasQueryFilter(TenantFilterName, e => e.Tenant == VisibleTenant);
    }
}

public sealed class SoftDeleteFilterCompositionTests
{
    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new TestClock());
        services.AddSingleton<ICurrentUser>(new TestCurrentUser { UserId = Guid.CreateVersion7() });
        services.AddBedrockPersistence<ComposeFilterDbContext>(options => options.UseSqlite(connection));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ComposeFilterDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    [Fact]
    public async Task Module_named_filter_composes_with_soft_delete_filter_instead_of_overriding()
    {
        var (provider, connection) = await BuildAsync();
        try
        {
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ComposeFilterDbContext>();
                db.Items.Add(new TenantScopedThing { Tenant = "acme" });                          // hiện
                db.Items.Add(new TenantScopedThing { Tenant = "acme", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow }); // bị soft-delete lọc
                db.Items.Add(new TenantScopedThing { Tenant = "other" });                          // bị tenant lọc
                await db.SaveChangesAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ComposeFilterDbContext>();

                // Cả HAI filter cùng áp (AND): chỉ còn 1 bản ghi acme & chưa xóa.
                var visible = await db.Items.ToListAsync();
                var only = Assert.Single(visible);
                Assert.Equal("acme", only.Tenant);
                Assert.False(only.IsDeleted);

                // Bỏ mọi filter → thấy toàn bộ 3 bản ghi (bằng chứng 2 bản ghi kia bị FILTER, không mất).
                Assert.Equal(3, await db.Items.IgnoreQueryFilters().CountAsync());
            }
        }
        finally
        {
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
