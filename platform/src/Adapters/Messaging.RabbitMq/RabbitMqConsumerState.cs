using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Adapters.Messaging.RabbitMq;

public enum RabbitMqConsumerStatus
{
    Stopped,
    Connecting,
    TopologyReady,
    Consuming,
    Recovering,
    Stopping,
    Faulted,
}

public sealed record RabbitMqConsumerSnapshot(
    string QueueName,
    RabbitMqConsumerStatus Status,
    string? ConsumerTag);

/// <summary>Thread-safe runtime state shared by one consumer instance and its readiness check.</summary>
public sealed class RabbitMqConsumerState(string queueName)
{
    private readonly Lock _gate = new();
    private RabbitMqConsumerStatus _status = RabbitMqConsumerStatus.Stopped;
    private string? _consumerTag;

    public RabbitMqConsumerSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new RabbitMqConsumerSnapshot(queueName, _status, _consumerTag);
        }
    }

    internal void TransitionTo(RabbitMqConsumerStatus status, string? consumerTag = null)
    {
        lock (_gate)
        {
            _status = status;
            _consumerTag = status == RabbitMqConsumerStatus.Consuming ? consumerTag : null;
        }
    }
}

internal sealed class RabbitMqConsumerHealthCheck(RabbitMqConsumerState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = state.Snapshot();
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["queue"] = snapshot.QueueName,
            ["status"] = snapshot.Status.ToString(),
        };

        return Task.FromResult(
            snapshot.Status == RabbitMqConsumerStatus.Consuming
            && !string.IsNullOrWhiteSpace(snapshot.ConsumerTag)
                ? HealthCheckResult.Healthy("RabbitMQ consumer is consuming.", data)
                : HealthCheckResult.Unhealthy("RabbitMQ consumer is not consuming.", data: data));
    }
}
