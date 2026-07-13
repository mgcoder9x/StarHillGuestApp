using Adapters.Messaging.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

public sealed class RabbitMqConsumerHealthCheckTests
{
    [Theory]
    [InlineData(RabbitMqConsumerStatus.Stopped)]
    [InlineData(RabbitMqConsumerStatus.Connecting)]
    [InlineData(RabbitMqConsumerStatus.TopologyReady)]
    [InlineData(RabbitMqConsumerStatus.Recovering)]
    [InlineData(RabbitMqConsumerStatus.Stopping)]
    [InlineData(RabbitMqConsumerStatus.Faulted)]
    public async Task Non_consuming_states_are_unhealthy(RabbitMqConsumerStatus status)
    {
        var state = new RabbitMqConsumerState("identity.queue");
        state.TransitionTo(status);

        var result = await new RabbitMqConsumerHealthCheck(state)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal(status.ToString(), result.Data["status"]);
    }

    [Fact]
    public async Task Consuming_with_broker_consumer_tag_is_healthy()
    {
        var state = new RabbitMqConsumerState("identity.queue");
        state.TransitionTo(RabbitMqConsumerStatus.Consuming, "amq.ctag-123");

        var result = await new RabbitMqConsumerHealthCheck(state)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("identity.queue", result.Data["queue"]);
    }

    [Fact]
    public async Task Consuming_without_consumer_tag_is_unhealthy()
    {
        var state = new RabbitMqConsumerState("identity.queue");
        state.TransitionTo(RabbitMqConsumerStatus.Consuming);

        var result = await new RabbitMqConsumerHealthCheck(state)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
