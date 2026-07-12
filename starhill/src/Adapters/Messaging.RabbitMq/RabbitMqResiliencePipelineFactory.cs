using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Dựng resilience pipeline biên adapter (design §9.2, F33): <c>timeout → retry (exponential + jitter) →
/// circuit-breaker</c> (thứ tự add = ngoài→trong: timeout bọc ngoài cùng). Áp CHỈ ở biên adapter, KHÔNG ở
/// lõi/use case. Retry an toàn vì publish là idempotent ở mức hệ thống (Inbox dedup phía consumer — §4.5).
/// Tách factory THUẦN để test hành vi retry KHÔNG cần broker.
/// </summary>
internal static class RabbitMqResiliencePipelineFactory
{
    public static ResiliencePipeline Create(RabbitMqResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.RetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs),
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitFailureRatio,
                MinimumThroughput = options.CircuitMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakSeconds),
            })
            .Build();
    }
}
