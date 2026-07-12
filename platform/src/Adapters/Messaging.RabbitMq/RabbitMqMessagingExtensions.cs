using Bedrock.Application.Messaging.Dispatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Wire adapter RabbitMQ ở HOST (F29): <c>AddMessagingCore().AddRabbitMqMessaging(cfg)</c> — bỏ dòng adapter =
/// quay về default fail-loud (<c>ThrowingEventBusPublisher</c>), KHÔNG sửa lõi. Dùng <c>Replace</c> để OVERRIDE
/// đúng MỘT registration (duplicate-guard task 10 không báo nhầm). Validate-on-start (F35): cấu hình sai chặn boot.
/// </summary>
public static class RabbitMqMessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new RabbitMqOptions();
        configuration.GetSection(RabbitMqOptions.SectionName).Bind(options);
        RabbitMqOptions.Validate(options);
        services.AddSingleton(options);

        // OVERRIDE default (AddMessagingCore đăng ký ThrowingEventBusPublisher). Singleton: connection dùng chung.
        services.Replace(ServiceDescriptor.Singleton<IEventBusPublisher, RabbitMqEventBusPublisher>());

        // Readiness (design §9.6/R34): messaging bật → broker là dependency (publish+consume) → check tag "ready"
        // chảy vào /health/ready. Broker chết → readiness 503 → orchestrator ngừng route (không dồn ứ outbox âm thầm).
        services.AddHealthChecks()
            .AddTypeActivatedCheck<RabbitMqHealthCheck>(
                name: "rabbitmq", failureStatus: HealthStatus.Unhealthy, tags: ["ready"]);
        return services;
    }

    /// <summary>
    /// Wire SUBSCRIBER RabbitMQ (BackgroundService) tiêu thụ event từ queue → port <c>IIntegrationEventDispatcher</c>
    /// (impl agnostic ở Bedrock.Infrastructure). Yêu cầu đã gọi <see cref="AddRabbitMqMessaging"/> (RabbitMqOptions
    /// kết nối/exchange) + phía Bedrock.Infrastructure đã <c>AddIntegrationEventConsumer()</c> + registry + handler.
    /// TOPOLOGY (queue/binding/prefetch) do Host khai qua <paramref name="configure"/> (N-063). Validate-on-start (F35).
    /// </summary>
    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services, Action<RabbitMqConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var consumerOptions = new RabbitMqConsumerOptions();
        configure(consumerOptions);
        RabbitMqConsumerOptions.Validate(consumerOptions);
        // Mỗi call giữ options riêng trong factory, cho phép nhiều module/queue cùng Host mà không last-wins
        // trên một RabbitMqConsumerOptions singleton.
        services.AddSingleton<IHostedService>(sp =>
            ActivatorUtilities.CreateInstance<RabbitMqConsumer>(sp, consumerOptions));
        return services;
    }
}
