using Bedrock.Application.Events;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bedrock.Infrastructure.DependencyInjection;

/// <summary>
/// Wire persistence nền TƯỜNG MINH (design §5.7/§9.6): DbContext dẫn xuất <typeparamref name="TContext"/>,
/// snake_case, clock, dispatcher, repo/UoW scoped (cùng scope = cùng DbContext = một điểm ghi), và DB
/// readiness check (tag <c>ready</c>, timeout 5s). App gọi trong <c>AddXxxModule(cfg)</c>/Host một dòng.
/// </summary>
public static class BedrockPersistenceExtensions
{
    private const string DatabaseHealthCheckName = "database";

    public static IServiceCollection AddBedrockPersistence<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // Clock singleton (không state); dispatcher scoped (cùng scope với DbContext → handler chung transaction).
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Outbox writer (use case chỉ thấy IOutboxWriter — CP11). Scoped: dùng chung PlatformDbContext/scope
        // → EnqueueAsync ghi outbox CÙNG transaction với state. Bảng outbox chỉ tồn tại nếu module gọi
        // modelBuilder.AddOutboxInbox() trong DbContext của nó (per-module opt-in — design §4.6).
        services.TryAddScoped<IOutboxWriter, EfOutboxWriter>();

        services.AddDbContext<TContext>((_, options) =>
        {
            configureDbContext(options);
            options.UseSnakeCaseNamingConvention();
        });

        // PlatformDbContext (base) resolve về CHÍNH instance TContext trong scope → repo/UoW dùng chung ChangeTracker.
        services.AddScoped<PlatformDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        // Readiness: DB check gắn tag "ready" (design §9.6 / R34); timeout 5s (task 6.3).
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>(name: DatabaseHealthCheckName, tags: ["ready"]);

        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                if (string.Equals(registration.Name, DatabaseHealthCheckName, StringComparison.Ordinal))
                {
                    registration.Timeout = TimeSpan.FromSeconds(5);
                }
            }
        });

        return services;
    }
}
