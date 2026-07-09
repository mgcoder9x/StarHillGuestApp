using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
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
        : PlatformDbContext(options, clock, currentUser, dispatcher);

    [Fact]
    public void Two_modules_get_distinct_ready_health_check_names_and_service_resolves()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBedrockPersistence<TestDbContext>(o => o.UseSqlite("DataSource=m1;Mode=Memory"));
        services.AddBedrockPersistence<SecondaryDbContext>(o => o.UseSqlite("DataSource=m2;Mode=Memory"));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var readyChecks = registrations.Where(r => r.Tags.Contains("ready")).ToList();

        Assert.Equal(2, readyChecks.Count);
        Assert.Equal(2, readyChecks.Select(r => r.Name).Distinct(StringComparer.Ordinal).Count()); // tên DUY NHẤT

        // Resolve HealthCheckService: ctor DefaultHealthCheckService validate trùng tên → KHÔNG được ném (AD-042).
        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }
}
