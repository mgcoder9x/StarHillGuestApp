using Adapters.Messaging.RabbitMq;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// Guard LOCAL (không broker) cho <see cref="RabbitMqConsumerOptions"/> — A-03: fail-fast cấu hình subscriber
/// (validate-on-start chặn boot khi sai) + hợp đồng đặt tên quarantine (DLQ). Hành vi DLX/quarantine runtime
/// (NACK requeue=false → DLX) chứng minh trên broker thật ở `RabbitMqConsumeEndToEndTests` (Testcontainers/CI).
/// </summary>
public sealed class RabbitMqConsumerOptionsTests
{
    private static RabbitMqConsumerOptions Valid()
    {
        var options = new RabbitMqConsumerOptions { QueueName = "starhill.identity" };
        options.RoutingKeys.Add("identity.#");
        return options;
    }

    [Fact]
    public void Valid_options_pass()
    {
        RabbitMqConsumerOptions.Validate(Valid()); // không ném
    }

    [Fact]
    public void Empty_queue_name_fails()
    {
        var options = Valid();
        options.QueueName = "  ";
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void No_routing_keys_fails()
    {
        var options = new RabbitMqConsumerOptions { QueueName = "q" }; // RoutingKeys rỗng
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Whitespace_routing_key_fails()
    {
        var options = new RabbitMqConsumerOptions { QueueName = "q" };
        options.RoutingKeys.Add("  ");
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Zero_prefetch_fails()
    {
        var options = Valid();
        options.PrefetchCount = 0;
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Whitespace_dead_letter_exchange_fails()
    {
        // DLX rỗng = mất đường quarantine → NACK requeue=false sẽ DROP. Fail-fast chặn cấu hình nguy hiểm này (A-03).
        var options = Valid();
        options.DeadLetterExchangeName = "  ";
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Effective_dead_letter_queue_defaults_to_queue_suffix()
    {
        var options = Valid();
        Assert.Equal("starhill.identity.dead-letter", options.EffectiveDeadLetterQueueName);

        options.DeadLetterQueueName = "custom.dlq";
        Assert.Equal("custom.dlq", options.EffectiveDeadLetterQueueName);
    }

    [Fact]
    public void Effective_consumer_name_defaults_to_queue_name()
    {
        var options = Valid();
        Assert.Equal("starhill.identity", options.EffectiveConsumerName); // ổn định qua restart → khoá idempotency inbox

        options.ConsumerName = "identity-worker";
        Assert.Equal("identity-worker", options.EffectiveConsumerName);
    }

    [Fact]
    public void Default_dead_letter_exchange_is_derived_per_queue()
    {
        var identity = Valid();
        var billing = new RabbitMqConsumerOptions { QueueName = "starhill.billing" };
        billing.RoutingKeys.Add("billing.#");

        Assert.Equal("starhill.identity.dead-letter", identity.EffectiveDeadLetterExchangeName);
        Assert.Equal("starhill.billing.dead-letter", billing.EffectiveDeadLetterExchangeName);
        Assert.NotEqual(identity.EffectiveDeadLetterExchangeName, billing.EffectiveDeadLetterExchangeName);
    }

    [Fact]
    public void Zero_or_negative_max_delivery_attempts_fails()
    {
        // P0-02: MaxDeliveryAttempts < 1 = không lần giao nào → mọi transient rơi thẳng DLQ (retry tier vô nghĩa). Fail-fast.
        var options = Valid();
        options.MaxDeliveryAttempts = 0;
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));

        options.MaxDeliveryAttempts = -1;
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Non_positive_retry_delay_fails()
    {
        // P0-02: RetryDelay <= 0 → x-message-ttl = 0 = message dead-letter tức thì → hot-loop retry không delay. Fail-fast.
        var options = Valid();
        options.RetryDelay = TimeSpan.Zero;
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));

        options.RetryDelay = TimeSpan.FromSeconds(-1);
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Whitespace_retry_exchange_fails()
    {
        var options = Valid();
        options.RetryExchangeName = "  ";
        Assert.Throws<InvalidOperationException>(() => RabbitMqConsumerOptions.Validate(options));
    }

    [Fact]
    public void Effective_retry_queue_defaults_to_queue_suffix()
    {
        var options = Valid();
        Assert.Equal("starhill.identity.retry", options.EffectiveRetryQueueName);

        options.RetryQueueName = "custom.retry";
        Assert.Equal("custom.retry", options.EffectiveRetryQueueName);
    }

    [Fact]
    public void Retry_defaults_are_sane()
    {
        var options = Valid();
        Assert.Equal(5, options.MaxDeliveryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), options.RetryDelay);
        Assert.Equal("starhill.identity.retry", options.EffectiveRetryExchangeName);
        Assert.Equal("x-bedrock-attempt", RabbitMqConsumerOptions.AttemptHeader);
    }

    [Fact]
    public void Two_queues_get_isolated_retry_topology_by_default()
    {
        var identity = Valid();
        var billing = new RabbitMqConsumerOptions { QueueName = "starhill.billing" };
        billing.RoutingKeys.Add("billing.#");

        Assert.NotEqual(identity.EffectiveRetryExchangeName, billing.EffectiveRetryExchangeName);
        Assert.NotEqual(identity.EffectiveRetryQueueName, billing.EffectiveRetryQueueName);
        Assert.NotEqual(identity.EffectiveDeadLetterExchangeName, billing.EffectiveDeadLetterExchangeName);
        Assert.NotEqual(identity.EffectiveDeadLetterQueueName, billing.EffectiveDeadLetterQueueName);
    }
}
