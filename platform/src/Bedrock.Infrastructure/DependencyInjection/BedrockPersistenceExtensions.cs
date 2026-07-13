using Bedrock.Application.DependencyInjection;
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
/// readiness check (tag <c>ready</c>, timeout 5s). Outbox, Inbox và RefreshToken là capability opt-in riêng;
/// module chỉ đăng ký capability tương ứng với bảng mà DbContext thực sự map.
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

        // PlatformDbContext (base) resolve về CHÍNH instance TContext trong scope → repo/UoW dùng chung ChangeTracker.
        services.AddScoped<PlatformDbContext>(sp => sp.GetRequiredService<TContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.TryAddScoped<IUnitOfWorkResolver, ServiceProviderUnitOfWorkResolver>();
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
        services.TryAddScoped<IUnitOfWorkResolver, ServiceProviderUnitOfWorkResolver>();
        AddDatabaseHealthCheck<TContext>(services);
        return services;
    }

    /// <summary>
    /// Opt-in Outbox producer cho host một DbContext. DbContext phải map <c>AddOutboxInbox</c> và đã được
    /// đăng ký bằng <see cref="AddBedrockPersistence{TContext}(IServiceCollection,Action{DbContextOptionsBuilder})"/>.
    /// </summary>
    public static IServiceCollection AddBedrockOutbox<TContext>(this IServiceCollection services)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireUnkeyed(typeof(TContext));
        if (registrations.TryAddCapability(null, typeof(TContext), PersistenceCapability.Outbox))
        {
            services.TryAddScoped<IOutboxWriter>(sp => new EfOutboxWriter(sp.GetRequiredService<TContext>()));
        }

        // P1-15: đánh dấu context này CÓ outbox producer → startup guard đòi phải có dispatcher worker (hoặc offline tường minh).
        services.BedrockStartupValidation().RegisterOutboxProducer(typeof(TContext));
        return services;
    }

    /// <summary>Opt-in Outbox producer cho một module keyed.</summary>
    public static IServiceCollection AddBedrockOutbox<TContext>(
        this IServiceCollection services,
        string moduleKey)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireKeyed(moduleKey, typeof(TContext));
        if (registrations.TryAddCapability(moduleKey, typeof(TContext), PersistenceCapability.Outbox))
        {
            services.TryAddKeyedScoped<IOutboxWriter>(
                moduleKey,
                (sp, _) => new EfOutboxWriter(sp.GetRequiredService<TContext>()));
        }

        // P1-15: producer keyed cũng đánh dấu theo context (drainer AddOutboxDispatcherWorker<TContext> khớp theo context).
        services.BedrockStartupValidation().RegisterOutboxProducer(typeof(TContext));
        return services;
    }

    /// <summary>
    /// Opt-in Inbox store cho host một DbContext. Dùng cùng scope/DbContext với consumer handler để inbox mark và
    /// business state commit nguyên tử.
    /// </summary>
    public static IServiceCollection AddBedrockInbox<TContext>(this IServiceCollection services)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireUnkeyed(typeof(TContext));
        if (registrations.TryAddCapability(null, typeof(TContext), PersistenceCapability.Inbox))
        {
            services.TryAddScoped<IInboxStore>(sp => new EfInboxStore(
                sp.GetRequiredService<TContext>(),
                sp.GetRequiredService<IClock>()));
        }

        return services;
    }

    /// <summary>Opt-in Inbox store cho một module keyed.</summary>
    public static IServiceCollection AddBedrockInbox<TContext>(
        this IServiceCollection services,
        string moduleKey)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireKeyed(moduleKey, typeof(TContext));
        if (registrations.TryAddCapability(moduleKey, typeof(TContext), PersistenceCapability.Inbox))
        {
            services.TryAddKeyedScoped<IInboxStore>(
                moduleKey,
                (sp, _) => new EfInboxStore(
                    sp.GetRequiredService<TContext>(),
                    sp.GetRequiredService<IClock>()));
        }

        return services;
    }

    /// <summary>Opt-in RefreshToken store cho host một DbContext đã map <c>AddRefreshTokens</c>.</summary>
    public static IServiceCollection AddBedrockRefreshTokens<TContext>(this IServiceCollection services)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireUnkeyed(typeof(TContext));
        if (registrations.TryAddCapability(null, typeof(TContext), PersistenceCapability.RefreshTokens))
        {
            services.TryAddScoped<IRefreshTokenStore>(sp => new EfRefreshTokenStore(
                sp.GetRequiredService<TContext>(),
                sp.GetRequiredService<IClock>()));
        }

        return services;
    }

    /// <summary>Opt-in RefreshToken store cho một module keyed.</summary>
    public static IServiceCollection AddBedrockRefreshTokens<TContext>(
        this IServiceCollection services,
        string moduleKey)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);
        var registrations = GetOrCreatePersistenceRegistrations(services);
        registrations.RequireKeyed(moduleKey, typeof(TContext));
        if (registrations.TryAddCapability(moduleKey, typeof(TContext), PersistenceCapability.RefreshTokens))
        {
            services.TryAddKeyedScoped<IRefreshTokenStore>(
                moduleKey,
                (sp, _) => new EfRefreshTokenStore(
                    sp.GetRequiredService<TContext>(),
                    sp.GetRequiredService<IClock>()));
        }

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
        private readonly HashSet<Type> _registeredContexts = [];
        private readonly HashSet<(string? Key, Type Context, PersistenceCapability Capability)> _capabilities = [];

        public void AddUnkeyed(Type contextType)
        {
            // P1-10 (bijection): mỗi DbContext đăng ký ĐÚNG một lần — chặn TRƯỚC (cùng context vừa keyed vừa unkeyed).
            EnsureContextNotAlreadyRegistered(contextType);

            if (_unkeyedContext is null)
            {
                _unkeyedContext = contextType;
                _registeredContexts.Add(contextType);
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

            // P1-10 (bijection): cùng DbContext KHÔNG được đăng ký dưới nhiều key (gây AddDbContext trùng → options last-wins).
            EnsureContextNotAlreadyRegistered(contextType);

            _keyedContexts.Add(moduleKey, contextType);
            _registeredContexts.Add(contextType);
        }

        /// <summary>
        /// P1-10: enforce BIJECTION context↔registration. Cùng một <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
        /// type đăng ký persistence NHIỀU LẦN (nhiều key, hoặc vừa keyed vừa unkeyed) → <c>AddDbContext&lt;TContext&gt;</c>
        /// bị gọi trùng với các configure delegate khác nhau → options phụ thuộc THỨ TỰ (last-wins) = cùng lớp rủi ro
        /// last-registration-wins (P0-1). Fail-fast: mỗi context một registration. Cần chia sẻ dữ liệu → tách DbContext.
        /// </summary>
        private void EnsureContextNotAlreadyRegistered(Type contextType)
        {
            if (_registeredContexts.Contains(contextType))
            {
                throw new InvalidOperationException(
                    $"DbContext '{contextType.FullName}' đã được đăng ký persistence trước đó. Mỗi DbContext PHẢI ánh "
                    + "xạ ĐÚNG một module key (bijection) — đăng ký cùng context dưới nhiều key hoặc vừa keyed vừa "
                    + "unkeyed sẽ gọi AddDbContext trùng (options last-wins). Nếu nhiều module cần dữ liệu chung, tách "
                    + "DbContext riêng cho từng module.");
            }
        }

        public void RequireUnkeyed(Type contextType)
        {
            if (_unkeyedContext != contextType)
            {
                throw new InvalidOperationException(
                    $"Capability cho '{contextType.FullName}' yêu cầu gọi AddBedrockPersistence<{contextType.Name}> "
                    + "trước, bằng overload không module key.");
            }
        }

        public void RequireKeyed(string moduleKey, Type contextType)
        {
            if (!_keyedContexts.TryGetValue(moduleKey, out var registeredContext) || registeredContext != contextType)
            {
                var actual = registeredContext?.FullName ?? "chưa đăng ký";
                throw new InvalidOperationException(
                    $"Capability module '{moduleKey}' yêu cầu persistence '{contextType.FullName}', nhưng key này "
                    + $"đang trỏ tới '{actual}'. Gọi AddBedrockPersistence với đúng key/context trước.");
            }
        }

        public bool TryAddCapability(
            string? moduleKey,
            Type contextType,
            PersistenceCapability capability) =>
            _capabilities.Add((moduleKey, contextType, capability));
    }

    private enum PersistenceCapability
    {
        Outbox,
        Inbox,
        RefreshTokens,
    }
}
