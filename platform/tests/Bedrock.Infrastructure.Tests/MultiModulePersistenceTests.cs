using Bedrock.Application.Events;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// GUARD AD-042 (anti-drift L3): nhiều module cùng gọi <c>AddBedrockPersistence</c> với DbContext KHÁC NHAU →
/// tên DB health-check PHẢI duy nhất per-context (không hardcode "database"), nếu không
/// <c>DefaultHealthCheckService</c> ném "duplicate registration" lúc resolve → crash boot multi-module (N-040).
/// </summary>
public sealed class MultiModulePersistenceTests
{
    // Context thứ hai (giả lập module khác) — chỉ cần dẫn xuất PlatformDbContext, không cần DbSet.
    private sealed class SecondaryDbContext(
        DbContextOptions<SecondaryDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher dispatcher)
        : PlatformDbContext(options, clock, currentUser, dispatcher)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddOutboxInbox(isNpgsql: Database.IsNpgsql());
        }
    }

    private sealed record ModuleEvent(Guid Id, DateTimeOffset OccurredAt, string Name)
        : IntegrationEvent(Id, OccurredAt)
    {
        public override string EventType => "test.module_event";
    }

    [Fact]
    public async Task Two_keyed_modules_resolve_distinct_persistence_ports_and_outbox_contexts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser());
        services.AddBedrockPersistence<TestDbContext>("module-1", o => o.UseSqlite("DataSource=m1;Mode=Memory"));
        services.AddBedrockPersistence<SecondaryDbContext>("module-2", o => o.UseSqlite("DataSource=m2;Mode=Memory"));
        services.AddBedrockOutbox<TestDbContext>("module-1");
        services.AddBedrockOutbox<SecondaryDbContext>("module-2");

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var readyChecks = registrations.Where(r => r.Tags.Contains("ready")).ToList();

        Assert.Equal(2, readyChecks.Count);
        Assert.Equal(2, readyChecks.Select(r => r.Name).Distinct(StringComparer.Ordinal).Count()); // tên DUY NHẤT

        // Resolve HealthCheckService: ctor DefaultHealthCheckService validate trùng tên → KHÔNG được ném (AD-042).
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        Assert.NotNull(healthCheckService);

        await using var scope = provider.CreateAsyncScope();
        var writer1 = scope.ServiceProvider.GetRequiredKeyedService<IOutboxWriter>("module-1");
        var writer2 = scope.ServiceProvider.GetRequiredKeyedService<IOutboxWriter>("module-2");
        var context1 = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var context2 = scope.ServiceProvider.GetRequiredService<SecondaryDbContext>();

        await writer1.EnqueueAsync(new ModuleEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow, "one"));
        Assert.Single(context1.ChangeTracker.Entries<OutboxMessage>());
        Assert.Empty(context2.ChangeTracker.Entries<OutboxMessage>());

        await writer2.EnqueueAsync(new ModuleEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow, "two"));
        Assert.Single(context1.ChangeTracker.Entries<OutboxMessage>());
        Assert.Single(context2.ChangeTracker.Entries<OutboxMessage>());
    }

    [Fact]
    public void Two_unkeyed_contexts_fail_fast_instead_of_last_registration_wins()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBedrockPersistence<TestDbContext>(o => o.UseSqlite("DataSource=m1;Mode=Memory"));

        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddBedrockPersistence<SecondaryDbContext>(o => o.UseSqlite("DataSource=m2;Mode=Memory")));

        Assert.Contains("moduleKey", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Foundation_does_not_register_schema_dependent_capabilities()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser());
        services.AddBedrockPersistence<TestDbContext>(o => o.UseSqlite("DataSource=:memory:"));

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        Assert.Null(scope.ServiceProvider.GetService<IOutboxWriter>());
        Assert.Null(scope.ServiceProvider.GetService<IInboxStore>());
        Assert.Null(scope.ServiceProvider.GetService<IRefreshTokenStore>());
    }

    [Fact]
    public void Capability_fails_fast_when_module_key_points_to_another_context()
    {
        var services = new ServiceCollection();
        services.AddBedrockPersistence<TestDbContext>("module-1", o => o.UseSqlite("DataSource=:memory:"));

        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddBedrockInbox<SecondaryDbContext>("module-1"));

        Assert.Contains(typeof(SecondaryDbContext).FullName!, error.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(TestDbContext).FullName!, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unit_of_work_resolver_rejects_whitespace_key_instead_of_falling_back_to_unkeyed()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser());
        services.AddBedrockPersistence<TestDbContext>("module-1", o => o.UseSqlite("DataSource=:memory:"));

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IUnitOfWorkResolver>();

        Assert.Throws<ArgumentException>(() => resolver.Resolve("  "));
    }
}
