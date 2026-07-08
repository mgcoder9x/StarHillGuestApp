using Bedrock.Messaging.Contracts;
using Xunit;

namespace Bedrock.UnitTests.Messaging;

public sealed class IntegrationEventTests
{
    private sealed record SampleIntegrationEvent(Guid Id, DateTimeOffset OccurredAt) : IntegrationEvent(Id, OccurredAt)
    {
        public override string EventType => "sample.event";
    }

    [Fact]
    public void SchemaVersion_should_default_to_one()
    {
        var evt = new SampleIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.Equal(1, evt.SchemaVersion);
    }

    [Fact]
    public void EventType_should_be_stable_string_provided_by_derived_type()
    {
        var evt = new SampleIntegrationEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.Equal("sample.event", evt.EventType);
    }
}
