using System.Text;
using Bedrock.Application.Messaging.Dispatch;
using RabbitMQ.Client;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Ánh xạ THUẦN <see cref="OutgoingIntegrationMessage"/> → primitives RabbitMQ (routing key / body / properties).
/// Tách khỏi I/O để test được KHÔNG cần broker. Adapter mỏng: publish payload THÔ + headers (không cần CLR type
/// — §5.2); consumer dùng <c>IIntegrationEventTypeRegistry</c> deserialize.
/// </summary>
internal static class RabbitMqMessageMapper
{
    public const string ContentType = "application/json";

    public const string EventTypeHeader = "event-type";

    public const string SchemaVersionHeader = "schema-version";

    /// <summary>Routing key = EventType → consumer bind theo pattern topic (vd <c>identity.*</c>).</summary>
    public static string RoutingKeyOf(OutgoingIntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return message.EventType;
    }

    public static ReadOnlyMemory<byte> BodyOf(OutgoingIntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Encoding.UTF8.GetBytes(message.Payload);
    }

    public static BasicProperties PropertiesOf(OutgoingIntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            ContentType = ContentType,
            Persistent = true, // message bền (survive broker restart) — khớp đảm bảo at-least-once của Outbox.
            Headers = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [EventTypeHeader] = message.EventType,
                [SchemaVersionHeader] = message.SchemaVersion,
            },
        };

        if (!string.IsNullOrEmpty(message.CorrelationId))
        {
            properties.CorrelationId = message.CorrelationId; // W3C trace đối soát xuyên bus (F34/F21).
        }

        return properties;
    }
}
