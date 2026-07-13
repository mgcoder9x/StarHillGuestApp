using System.Reflection;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bedrock.Infrastructure.DependencyInjection;

/// <summary>
/// Wire phía WORKER outbox/inbox (design §7.2/§7.3). Tách khỏi <c>AddBedrockPersistence</c> vì per-module +
/// cần adapter <see cref="IEventBusPublisher"/> (chỉ có ở Host khi bật messaging).
/// </summary>
public static class OutboxDispatcherExtensions
{
    /// <summary>
    /// Đăng ký dispatcher outbox cho DbContext <typeparamref name="TContext"/> (per-module).
    /// Yêu cầu <see cref="IEventBusPublisher"/> đã đăng ký (adapter, vd RabbitMQ) — nếu không, resolve sẽ
    /// fail-fast khi worker chạy. Named-options theo context cho phép mỗi module tinh chỉnh riêng.
    /// </summary>
    public static IServiceCollection AddOutboxDispatcher<TContext>(
        this IServiceCollection services,
        Action<OutboxDispatcherOptions>? configure = null)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OutboxDispatcherOptions>(OutboxDispatcherOptions.KeyFor<TContext>())
            .Configure(options => configure?.Invoke(options))
            .Validate(OutboxDispatcherOptions.IsValid, "Outbox dispatcher options không hợp lệ.")
            .ValidateOnStart();

        services.AddScoped<EfOutboxDispatcher<TContext>>();
        services.AddScoped<IOutboxDispatcher>(sp => sp.GetRequiredService<EfOutboxDispatcher<TContext>>());
        return services;
    }

    /// <summary>Đăng ký dispatcher keyed cho một module; worker vẫn resolve concrete theo TContext.</summary>
    public static IServiceCollection AddOutboxDispatcher<TContext>(
        this IServiceCollection services,
        string moduleKey,
        Action<OutboxDispatcherOptions>? configure = null)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        services.AddOptions<OutboxDispatcherOptions>(OutboxDispatcherOptions.KeyFor<TContext>())
            .Configure(options => configure?.Invoke(options))
            .Validate(OutboxDispatcherOptions.IsValid, "Outbox dispatcher options không hợp lệ.")
            .ValidateOnStart();
        services.AddScoped<EfOutboxDispatcher<TContext>>();
        services.AddKeyedScoped<IOutboxDispatcher>(
            moduleKey,
            (sp, _) => sp.GetRequiredService<EfOutboxDispatcher<TContext>>());
        return services;
    }

    /// <summary>
    /// (OPT-IN) Lên lịch chạy dispatcher outbox của <typeparamref name="TContext"/> như một
    /// <c>BackgroundService</c> poll định kỳ (<see cref="OutboxDispatcherHostedService{TContext}"/>).
    /// <para>
    /// Host GỌI TƯỜNG MINH khi muốn phát tự động — <c>AddBedrockPersistence</c>/<c>AddOutboxDispatcher</c>
    /// KHÔNG tự đăng ký worker. Nhờ vậy lịch vẫn do Host quyết (giữ AD-047: "Host lên lịch", một mô hình duy
    /// nhất); base chỉ cấp sẵn vỏ poll-loop đúng-đắn (scope mỗi lượt + không hạ host khi lỗi tạm + shutdown êm).
    /// </para>
    /// Yêu cầu đã gọi <see cref="AddOutboxDispatcher{TContext}"/> (đăng ký <see cref="IOutboxDispatcher"/>) và
    /// có <see cref="IEventBusPublisher"/> (adapter, vd RabbitMQ). Named-options theo context.
    /// </summary>
    public static IServiceCollection AddOutboxDispatcherWorker<TContext>(
        this IServiceCollection services,
        Action<OutboxDispatcherWorkerOptions>? configure = null)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OutboxDispatcherWorkerOptions>(OutboxDispatcherWorkerOptions.KeyFor<TContext>())
            .Configure(options => configure?.Invoke(options))
            .Validate(OutboxDispatcherWorkerOptions.IsValid, "Outbox worker PollInterval phải > 0.")
            .ValidateOnStart();

        services.AddHostedService<OutboxDispatcherHostedService<TContext>>();
        return services;
    }

    /// <summary>
    /// Đăng ký job retention outbox cho DbContext <typeparamref name="TContext"/> (per-module, task 7.5).
    /// Base chỉ cung cấp LOGIC (<see cref="EfOutboxRetention{TContext}"/>); Host lên lịch chạy định kỳ — nhất
    /// quán với dispatcher (base KHÔNG có hosted-service). Named-options theo context: mỗi module đặt TTL riêng.
    /// </summary>
    public static IServiceCollection AddOutboxRetention<TContext>(
        this IServiceCollection services,
        Action<OutboxRetentionOptions>? configure = null)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OutboxRetentionOptions>(OutboxRetentionOptions.KeyFor<TContext>())
            .Configure(options => configure?.Invoke(options))
            .Validate(OutboxRetentionOptions.IsValid, "Outbox retention TTL phải > 0.")
            .ValidateOnStart();

        services.AddScoped<EfOutboxRetention<TContext>>();
        return services;
    }

    /// <summary>
    /// Wire phía CONSUME agnostic (design §7.3): <see cref="IInboxStore"/> (idempotency) + core
    /// <see cref="IIntegrationEventDispatcher"/> (<see cref="EfIntegrationEventDispatcher"/>). Scoped: dùng chung
    /// <c>PlatformDbContext</c>/scope với handler → inbox mark + business nguyên tử. Yêu cầu đã có
    /// <see cref="AddBedrockPersistence"/> (IUnitOfWork) + <see cref="AddIntegrationEventRegistry"/> (registry) +
    /// handler (<c>IIntegrationEventHandler&lt;T&gt;</c>) do app đăng ký. Adapter transport (vd RabbitMQ subscriber)
    /// gọi port này per-message. KHÔNG tự đăng ký transport (Host chọn adapter + topology).
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IIntegrationEventDispatcher, EfIntegrationEventDispatcher>();
        return services;
    }

    /// <summary>
    /// Wire consumer theo module key để UoW/Inbox/handler của module không resolve nhầm DbContext hoặc handler
    /// của module khác. Các dependency keyed phải được đăng ký bởi AddBedrockPersistence(moduleKey, ...) và
    /// AddBedrockInbox(moduleKey).
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumer<TContext>(
        this IServiceCollection services,
        string moduleKey)
        where TContext : PlatformDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleKey);

        services.AddKeyedScoped<IIntegrationEventDispatcher>(
            moduleKey,
            (sp, _) => new EfIntegrationEventDispatcher(
                sp.GetRequiredKeyedService<Bedrock.Application.Ports.Persistence.IUnitOfWork>(moduleKey),
                sp.GetRequiredKeyedService<IInboxStore>(moduleKey),
                sp.GetRequiredService<IIntegrationEventTypeRegistry>(),
                sp,
                moduleKey));
        return services;
    }

    /// <summary>
    /// Build registry <c>EventType → CLR type</c> từ các assembly <c>*.Contracts</c> (Host gọi lúc boot).
    /// Singleton (map bất biến). EventType lạ → dead-letter phía consumer, không crash (R17.3).
    /// </summary>
    public static IServiceCollection AddIntegrationEventRegistry(
        this IServiceCollection services,
        params Assembly[] contractsAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contractsAssemblies);

        services.TryAddSingleton<IIntegrationEventTypeRegistry>(
            _ => new IntegrationEventTypeRegistry(contractsAssemblies));
        return services;
    }
}
