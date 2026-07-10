using Adapters.Messaging.RabbitMq;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// Guard resilience biên adapter (§9.2/F33): pipeline retry lỗi transient (exponential+jitter) rồi thành công;
/// hết lượt retry → ném lỗi gốc. Test THUẦN (không broker) — chứng minh retry ĐÃ được wire quanh publish.
/// </summary>
public sealed class RabbitMqResiliencePipelineTests
{
    private static RabbitMqResilienceOptions FastRetry(int attempts) => new()
    {
        TimeoutSeconds = 5,
        RetryAttempts = attempts,
        RetryBaseDelayMs = 1, // nhanh cho test
        CircuitMinimumThroughput = 100, // đủ cao để circuit KHÔNG mở trong test (chỉ kiểm retry)
    };

    [Fact]
    public async Task Retries_transient_failures_then_succeeds()
    {
        var pipeline = RabbitMqResiliencePipelineFactory.Create(FastRetry(attempts: 3));
        var attempts = 0;

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        });

        Assert.Equal(3, attempts); // 1 lần đầu + 2 retry = thành công ở lần 3.
    }

    [Fact]
    public async Task Exhausts_retries_then_rethrows_original()
    {
        var pipeline = RabbitMqResiliencePipelineFactory.Create(FastRetry(attempts: 2));
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                await Task.CompletedTask.ConfigureAwait(false);
                throw new InvalidOperationException("always");
            }));

        Assert.Equal(3, attempts); // 1 lần đầu + 2 retry rồi ném.
    }
}
