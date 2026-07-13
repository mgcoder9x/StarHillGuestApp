using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging.Dispatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// Guard F29 "cắm không sửa lõi" ở tầng DI: <c>AddRabbitMqMessaging</c> OVERRIDE default <see cref="IEventBusPublisher"/>
/// bằng <c>Replace</c> (đúng MỘT registration → duplicate-guard không báo) + validate-on-start (cấu hình sai chặn boot).
/// </summary>
public sealed class AddRabbitMqMessagingTests
{
    private sealed class DummyDefaultPublisher : IEventBusPublisher
    {
        public Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static IConfiguration Config(string hostName = "localhost", string exchange = "test.events") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:HostName"] = hostName,
                ["RabbitMq:ExchangeName"] = exchange,
            })
            .Build();

    [Fact]
    public void Replaces_default_publisher_with_rabbitmq_single_registration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventBusPublisher, DummyDefaultPublisher>(); // giả lập default (AddMessagingCore).

        services.AddRabbitMqMessaging(Config());

        var publisherRegistrations = services.Where(d => d.ServiceType == typeof(IEventBusPublisher)).ToList();
        Assert.Single(publisherRegistrations); // Replace → đúng MỘT registration (không cộng dồn).
        Assert.Equal(typeof(RabbitMqEventBusPublisher), publisherRegistrations[0].ImplementationType);
        Assert.Contains(services, d => d.ServiceType == typeof(RabbitMqOptions));
    }

    [Fact]
    public void Invalid_config_fails_fast_at_registration()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddRabbitMqMessaging(Config(hostName: "")));
    }

    [Fact]
    public void Registers_rabbitmq_readiness_health_check_tagged_ready()
    {
        // design §9.6/R34: broker là dependency khi messaging bật → phải có check "rabbitmq" tag "ready" để
        // chảy vào /health/ready. Verify ĐĂNG KÝ (không cần Docker); hành vi Healthy/Unhealthy thật → integration.
        var services = new ServiceCollection();
        services.AddRabbitMqMessaging(Config());

        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var rabbit = Assert.Single(registrations, r => r.Name == "rabbitmq");
        Assert.Contains("ready", rabbit.Tags);
    }

    [Fact]
    public void Consumer_registration_adds_queue_specific_readiness_and_rejects_duplicate_queue()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddRabbitMqMessaging(Config());
        services.AddRabbitMqConsumer(options =>
        {
            options.QueueName = "identity.queue";
            options.RoutingKeys.Add("identity.#");
        });

        using var provider = services.BuildServiceProvider();
        Assert.Single(provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>());
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var consumerHealth = Assert.Single(registrations, r => r.Name == "rabbitmq-consumer:identity.queue");
        Assert.Contains("ready", consumerHealth.Tags);

        var error = Assert.Throws<InvalidOperationException>(() => services.AddRabbitMqConsumer(options =>
        {
            options.QueueName = "identity.queue";
            options.RoutingKeys.Add("identity.changed.#");
        }));
        Assert.Contains("đã được đăng ký", error.Message, StringComparison.Ordinal);
    }
}
