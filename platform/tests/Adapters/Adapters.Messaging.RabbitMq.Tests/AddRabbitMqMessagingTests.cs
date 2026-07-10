using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging.Dispatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public Task PublishAsync(OutboxMessage message, CancellationToken ct = default) => Task.CompletedTask;
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
}
