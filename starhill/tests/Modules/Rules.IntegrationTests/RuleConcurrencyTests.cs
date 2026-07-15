using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// CP15 (Req 8, optimistic concurrency) — hai admin sửa CÙNG một section Draft đồng thời: người ghi sau nhận xung đột
/// concurrency (KHÔNG ghi đè âm thầm). Chứng minh token <c>xmin</c> (map bởi <c>PlatformDbContext</c> cho entity
/// <c>IHasConcurrencyToken</c> trên Npgsql) đang HOẠT ĐỘNG trên <see cref="RuleSection"/> — mirror
/// <c>ResortConfigPersistenceTests</c> (CP15). BUỘC PostgreSQL thật (SQLite không có xmin) → Testcontainers; SKIP
/// nếu thiếu Docker (N-067). Đường use case (<c>UpdateRuleSectionUseCase</c>) surface xung đột này thành
/// <c>ConcurrencyConflictException</c> qua base <c>EfUnitOfWork</c> → middleware map 409.
/// </summary>
public sealed class RuleConcurrencyTests : IAsyncLifetime
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
        services.AddRulesInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rules")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        await db.Database.MigrateAsync();
    }

    [SkippableFact]
    public async Task Concurrent_section_update_second_writer_gets_concurrency_conflict()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        // Seed một RuleSet + RuleSection.
        Guid sectionId;
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var set = new RuleSet { ResortId = Guid.CreateVersion7(), UpdatedAt = DateTimeOffset.UnixEpoch };
            db.RuleSets.Add(set);
            var section = new RuleSection { RuleSetId = set.Id, Key = "welcome", SortOrder = 1 };
            db.RuleSections.Add(section);
            await db.SaveChangesAsync();
            sectionId = section.Id;
        }

        // Hai scope/DbContext độc lập đọc cùng bản ghi → cùng snapshot xmin.
        await using var scopeA = provider.CreateAsyncScope();
        await using var scopeB = provider.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<RulesDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<RulesDbContext>();

        var sectionA = await dbA.RuleSections.SingleAsync(s => s.Id == sectionId);
        var sectionB = await dbB.RuleSections.SingleAsync(s => s.Id == sectionId);

        sectionA.MinReadSeconds = 30;
        await dbA.SaveChangesAsync(); // Người ghi trước — thành công, xmin đổi.

        sectionB.MinReadSeconds = 60;
        // Người ghi sau — xmin cũ không còn khớp → concurrency conflict (không ghi đè âm thầm).
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }
}
