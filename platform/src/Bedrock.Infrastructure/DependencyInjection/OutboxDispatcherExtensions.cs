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
    /// Đăng ký dispatcher outbox cho DbContext <typeparamref name="TContext"/> (per-module) + inbox store.
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
            .Configure(options => configure?.Invoke(options));

        services.AddScoped<IOutboxDispatcher, EfOutboxDispatcher<TContext>>();
        services.TryAddScoped<IInboxStore, EfInboxStore>();
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
