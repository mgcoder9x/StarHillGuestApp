using Adapters.Messaging.RabbitMq;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

public sealed class RabbitMqOptionsTests
{
    private static RabbitMqOptions Valid() => new()
    {
        HostName = "localhost",
        Port = 5672,
        ExchangeName = "bedrock.events",
    };

    [Fact]
    public void Valid_options_pass()
    {
        RabbitMqOptions.Validate(Valid()); // không ném
    }

    [Fact]
    public void Empty_hostname_fails()
    {
        var options = Valid();
        options.HostName = "";
        Assert.Throws<InvalidOperationException>(() => RabbitMqOptions.Validate(options));
    }

    [Fact]
    public void Empty_exchange_fails()
    {
        var options = Valid();
        options.ExchangeName = "  ";
        Assert.Throws<InvalidOperationException>(() => RabbitMqOptions.Validate(options));
    }

    [Fact]
    public void Invalid_port_fails()
    {
        var options = Valid();
        options.Port = 0;
        Assert.Throws<InvalidOperationException>(() => RabbitMqOptions.Validate(options));
    }

    [Fact]
    public void Circuit_minimum_throughput_below_two_fails()
    {
        // Ràng buộc Polly: MinimumThroughput >= 2 → validate-on-start bắt sớm (fail-fast) thay vì nổ lúc build pipeline.
        var options = Valid();
        options.Resilience.CircuitMinimumThroughput = 1;
        Assert.Throws<InvalidOperationException>(() => RabbitMqOptions.Validate(options));
    }
}
