using System.Text;
using Bedrock.Application.Messaging.Dispatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// SUBSCRIBER RabbitMQ (F29 — "cắm không sửa lõi") = phía transport của consume, MIRROR
/// <see cref="RabbitMqEventBusPublisher"/> phía publish. <c>BackgroundService</c>: khai exchange (topic, durable) +
/// queue (durable) + binding theo <see cref="RabbitMqConsumerOptions.RoutingKeys"/>, QoS prefetch, rồi
/// <c>BasicConsumeAsync</c> (manual ack). MỖI delivery: tạo DI SCOPE riêng → resolve port
/// <see cref="IIntegrationEventDispatcher"/> (impl agnostic ở Infrastructure — adapter KHÔNG ref Infra, giữ CP3) →
/// dispatch → ACK/NACK theo kết quả.
/// <para>
/// <b>Chính sách ack (topology tối thiểu, N-063/AD sau):</b> Handled/Duplicate/DeadLettered → ACK (đã xử lý xong).
/// Handler lỗi (dispatch NÉM) → NACK requeue=<c>false</c>: TRÁNH hot-loop poison (requeue=true sẽ quay vòng vô hạn
/// khi handler luôn lỗi). Production NÊN cấu hình DLX qua <see cref="RabbitMqConsumerOptions.QueueArguments"/>
/// (<c>x-dead-letter-exchange</c>) để requeue=false ĐI VÀO DLX thay vì drop. Envelope hỏng (thiếu MessageId/EventType)
/// → ACK+log (redeliver không sửa được).
/// </para>
/// </summary>
public sealed partial class RabbitMqConsumer(
    RabbitMqOptions options,
    RabbitMqConsumerOptions consumerOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(options);

        _connection = await factory.CreateConnectionAsync(stoppingToken).ConfigureAwait(false);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

        await _channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        await _channel.QueueDeclareAsync(
            queue: consumerOptions.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: consumerOptions.QueueArguments.Count == 0 ? null : consumerOptions.QueueArguments,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        foreach (var routingKey in consumerOptions.RoutingKeys)
        {
            await _channel.QueueBindAsync(
                queue: consumerOptions.QueueName, exchange: options.ExchangeName, routingKey: routingKey,
                cancellationToken: stoppingToken).ConfigureAwait(false);
        }

        await _channel.BasicQosAsync(
            prefetchSize: 0, prefetchCount: consumerOptions.PrefetchCount, global: false,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: consumerOptions.QueueName, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        Log.ConsumerStarted(logger, consumerOptions.QueueName, consumerOptions.EffectiveConsumerName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown êm.
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel!;
        var deliveryTag = ea.DeliveryTag;

        // Envelope: MessageId (= IntegrationEvent.Id) + header event-type. Thiếu/hỏng → ACK+drop (redeliver vô ích).
        if (!Guid.TryParse(ea.BasicProperties.MessageId, out var messageId))
        {
            Log.MalformedEnvelope(logger, "MessageId", ea.BasicProperties.MessageId ?? "(null)");
            await channel.BasicAckAsync(deliveryTag, multiple: false).ConfigureAwait(false);
            return;
        }

        var eventType = DecodeHeader(ea.BasicProperties.Headers, RabbitMqMessageMapper.EventTypeHeader);
        if (string.IsNullOrEmpty(eventType))
        {
            Log.MalformedEnvelope(logger, RabbitMqMessageMapper.EventTypeHeader, "(null)");
            await channel.BasicAckAsync(deliveryTag, multiple: false).ConfigureAwait(false);
            return;
        }

        var incoming = new IncomingIntegrationMessage(
            messageId, consumerOptions.EffectiveConsumerName, eventType, ea.Body.ToArray());

#pragma warning disable CA1031 // Bắt rộng CÓ CHỦ ĐÍCH: lỗi handler/hạ tầng khi dispatch → NACK (redeliver/DLX), KHÔNG để ném ra dispatch loop của client (sẽ hạ consumer). Cách ly poison về broker (requeue=false).
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
            var outcome = await dispatcher.DispatchAsync(incoming).ConfigureAwait(false);

            if (outcome == InboxDispatchOutcome.DeadLettered)
            {
                Log.DeadLettered(logger, eventType, messageId);
            }

            await channel.BasicAckAsync(deliveryTag, multiple: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.DispatchFailed(logger, eventType, messageId, ex);
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false).ConfigureAwait(false);
        }
#pragma warning restore CA1031
    }

    private static string? DecodeHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        // AMQP field table: string được truyền dạng byte[] (longstr) → decode UTF-8.
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value.ToString(),
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "RabbitMQ consumer started (queue={Queue}, consumer={Consumer}).")]
        public static partial void ConsumerStarted(ILogger logger, string queue, string consumer);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
            Message = "Dead-lettered message (unknown event-type={EventType}, messageId={MessageId}).")]
        public static partial void DeadLettered(ILogger logger, string eventType, Guid messageId);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error,
            Message = "Dispatch failed (event-type={EventType}, messageId={MessageId}); NACK requeue=false.")]
        public static partial void DispatchFailed(ILogger logger, string eventType, Guid messageId, Exception exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
            Message = "Malformed envelope (missing/invalid {Field}={Value}); ACK-drop.")]
        public static partial void MalformedEnvelope(ILogger logger, string field, string value);
    }
}
