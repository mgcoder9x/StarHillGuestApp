using Bedrock.Application.Events;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Infrastructure.Persistence.Security;
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
    // Prefix tên DB health-check; tên THẬT gắn thêm tên TContext để DUY NHẤT per-module (AD-042).
    private const string DatabaseHealthCheckPrefix = "database";

    public static IServiceCollection AddBedrockPersistence<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.AddUnkeyed(typeof(TContext));

        AddPersistenceFoundation<TContext>(services, configureDbContext);

        // Outbox writer (use case chỉ thấy IOutboxWriter — CP11). Scoped: dùng chung PlatformDbContext/scope
        // → EnqueueAsync ghi outbox CÙNG transaction với state. Bảng outbox chỉ tồn tại nếu module gọi
        // modelBuilder.AddOutboxInbox() trong DbContext của nó (per-module opt-in — design §4.6).
        services.TryAddScoped<IOutboxWriter, EfOutboxWriter>();

        // Refresh-token store: cơ chế rotation nguyên tử ở lõi (AD-010), dùng chung PlatformDbContext/scope.
        // Chỉ hoạt động nếu module đã map bảng qua modelBuilder.AddRefreshTokens(schema) — per-module opt-in.
        services.TryAddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

        // PlatformDbContext (base) resolve về CHÍNH instance TContext trong scope → repo/UoW dùng chung ChangeTracker.
        services.AddScoped<PlatformDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        AddDatabaseHealthCheck<TContext>(services);

        return services;
    }

    /// <summary>
    /// Đăng ký persistence có key cho modular monolith nhiều DbContext. Mọi port phụ thuộc context được resolve
    /// bằng cùng <paramref name="moduleKey"/> nên không có last-registration-wins giữa các module.
    /// Repository aggregate đăng ký tường minh bằng <see cref="AddBedrockRepository{TContext,TEntity}"/>.
    /// </summary>
    public static IServiceCollection AddBedrockPersistence<TContext>(
        this IServiceCollection services,
        string moduleKey,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.AddKeyed(moduleKey, typeof(TContext));

        AddPersistenceFoundation<TContext>(services, configureDbContext);

        services.AddKeyedScoped<IUnitOfWork>(
            moduleKey,
            (sp, _) => new EfUnitOfWork(sp.GetRequiredService<TContext>()));
        services.AddKeyedScoped<IOutboxWriter>(
            moduleKey,
            (sp, _) => new EfOutboxWriter(sp.GetRequiredService<TContext>()));
        services.AddKeyedScoped<IRefreshTokenStore>(
            moduleKey,
            (sp, _) => new EfRefreshTokenStore(
                sp.GetRequiredService<TContext>(),
                sp.GetRequiredService<IClock>()));
        services.AddKeyedScoped<IInboxStore>(
            moduleKey,
            (sp, _) => new EfInboxStore(
                sp.GetRequiredService<TContext>(),
                sp.GetRequiredService<IClock>()));

        AddDatabaseHealthCheck<TContext>(services);
        return services;
    }

    /// <summary>Đăng ký repository aggregate theo đúng module key/DbContext; không dùng global context alias.</summary>
    public static IServiceCollection AddBedrockRepository<TContext, TEntity>(
        this IServiceCollection services,
        string moduleKey)
        where TContext : PlatformDbContext
        where TEntity : Bedrock.Domain.Entities.Entity
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        services.AddKeyedScoped<IRepository<TEntity>>(
            moduleKey,
            (sp, _) => new EfRepository<TEntity>(sp.GetRequiredService<TContext>()));
        return services;
    }

    private static void AddPersistenceFoundation<TContext>(
        IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TContext : PlatformDbContext
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddDbContext<TContext>((_, options) =>
        {
            configureDbContext(options);
            options.UseSnakeCaseNamingConvention();
        });
    }

    private static void AddDatabaseHealthCheck<TContext>(IServiceCollection services)
        where TContext : PlatformDbContext
    {

        // Readiness: DB check gắn tag "ready" (design §9.6 / R34); timeout 5s (task 6.3).
        // Tên PER-CONTEXT (AD-042): nhiều module cùng gọi AddBedrockPersistence với TContext khác nhau → tên
        // health-check khác nhau → KHÔNG trùng (health-check name PHẢI duy nhất, nếu không DefaultHealthCheckService
        // ném lúc resolve). Trước đây hardcode "database" → 2 module = crash boot (landmine multi-module, N-040).
        var healthCheckName = $"{DatabaseHealthCheckPrefix}:{typeof(TContext).Name}";
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>(name: healthCheckName, tags: ["ready"]);

        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                if (string.Equals(registration.Name, healthCheckName, StringComparison.Ordinal))
                {
                    registration.Timeout = TimeSpan.FromSeconds(5);
                }
            }
        });

    }

    private static PersistenceRegistrationRegistry GetOrCreatePersistenceRegistrations(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(PersistenceRegistrationRegistry))
            ?.ImplementationInstance as PersistenceRegistrationRegistry;
        if (existing is not null)
        {
            return existing;
        }

        var created = new PersistenceRegistrationRegistry();
        services.AddSingleton(created);
        return created;
    }

    private sealed class PersistenceRegistrationRegistry
    {
        private Type? _unkeyedContext;
        private readonly Dictionary<string, Type> _keyedContexts = new(StringComparer.Ordinal);

        public void AddUnkeyed(Type contextType)
        {
            if (_unkeyedContext is null)
            {
                _unkeyedContext = contextType;
                return;
            }

            throw new InvalidOperationException(
                $"Unkeyed AddBedrockPersistence chỉ hỗ trợ một DbContext nhưng đã có '{_unkeyedContext.FullName}' "
                + $"và đang thêm '{contextType.FullName}'. Dùng overload moduleKey cho modular monolith nhiều module.");
        }

        public void AddKeyed(string moduleKey, Type contextType)
        {
            if (_keyedContexts.TryGetValue(moduleKey, out var existing))
            {
                throw new InvalidOperationException(
                    $"Persistence module key '{moduleKey}' đã gắn với '{existing.FullName}', không thể gắn thêm "
                    + $"'{contextType.FullName}'. Module key phải duy nhất và ổn định.");
            }

            _keyedContexts.Add(moduleKey, contextType);
        }
    }
}
