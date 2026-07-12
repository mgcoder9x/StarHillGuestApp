using System.Text;
using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging.Dispatch;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// Guard mapping THUẦN OutgoingIntegrationMessage → RabbitMQ primitives (không cần broker). Khoá hợp đồng adapter
/// mỏng (§5.2): routing key = EventType, body = payload thô, headers mang event-type/schema-version, persistent.
/// </summary>
public sealed class RabbitMqMessageMapperTests
{
    private static OutgoingIntegrationMessage Message(string? correlationId = "corr-1") => new()
    {
        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        EventType = "identity.user_token_refreshed",
        SchemaVersion = 2,
        Payload = "{\"userId\":\"x\"}",
        OccurredAt = DateTimeOffset.UnixEpoch,
        CorrelationId = correlationId,
    };

    [Fact]
    public void RoutingKey_is_event_type()
    {
        Assert.Equal("identity.user_token_refreshed", RabbitMqMessageMapper.RoutingKeyOf(Message()));
    }

    [Fact]
    public void Body_is_utf8_payload()
    {
        var body = RabbitMqMessageMapper.BodyOf(Message());
        Assert.Equal("{\"userId\":\"x\"}", Encoding.UTF8.GetString(body.Span));
    }

    [Fact]
    public void Properties_carry_id_contenttype_persistent_and_headers()
    {
        var properties = RabbitMqMessageMapper.PropertiesOf(Message());

        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", properties.MessageId);
        Assert.Equal(RabbitMqMessageMapper.ContentType, properties.ContentType);
        Assert.True(properties.Persistent);
        Assert.Equal("corr-1", properties.CorrelationId);
        Assert.NotNull(properties.Headers);
        Assert.Equal("identity.user_token_refreshed", properties.Headers![RabbitMqMessageMapper.EventTypeHeader]);
        Assert.Equal(2, properties.Headers[RabbitMqMessageMapper.SchemaVersionHeader]);
    }

    [Fact]
    public void CorrelationId_absent_is_not_set()
    {
        var properties = RabbitMqMessageMapper.PropertiesOf(Message(correlationId: null));
        Assert.Null(properties.CorrelationId);
    }
}
