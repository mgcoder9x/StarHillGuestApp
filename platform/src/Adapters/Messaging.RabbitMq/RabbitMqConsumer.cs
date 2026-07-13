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
/// <b>Topology (consumer tự provision):</b> exchange topic (durable) + DLX topic (durable, effective dead-letter exchange)
/// + DLQ (durable, <see cref="RabbitMqConsumerOptions.EffectiveDeadLetterQueueName"/> bind "#") + queue chính có
/// <c>x-dead-letter-exchange</c> trỏ DLX. Nhờ vậy MỌI <c>BasicNack(requeue=false)</c> ĐI VÀO DLX (quarantine), KHÔNG drop.
/// </para>
/// <para>
/// <b>Chính sách ack (P0-02, phân tầng theo <see cref="RabbitMqDeliveryPolicy"/>):</b>
/// <list type="bullet">
///   <item>Handled/Duplicate → ACK (xử lý xong).</item>
///   <item>Lỗi PERMANENT — dispatch trả DeadLettered (unknown event-type/content-type/schema sai) HOẶC exception
///   <c>JsonException</c> (payload không deserialize được) → NACK requeue=<c>false</c> → DLX final NGAY (retry vô ích,
///   payload/envelope hỏng không tự lành).</item>
///   <item>Lỗi TRANSIENT (DB/network/timeout/handler tạm) — CÒN lượt (attempt &lt; <see cref="RabbitMqConsumerOptions.MaxDeliveryAttempts"/>)
///   → PUBLISH sang retry-exchange (kèm header <c>x-bedrock-attempt</c> = attempt+1, CONFIRM broker) rồi ACK bản gốc;
///   retry-queue giữ message hết <c>x-message-ttl</c> = RetryDelay rồi dead-letter QUAY LẠI main exchange (giữ routing
///   key gốc) → redeliver. HẾT lượt → NACK requeue=<c>false</c> → DLX final.</item>
///   <item>Envelope hỏng (thiếu/sai MessageId hoặc event-type header) → NACK requeue=<c>false</c> → DLX (payload hỏng
///   không sửa được → quarantine để soi).</item>
/// </list>
/// </para>
/// <para>
/// <b>Vì sao đếm attempt qua header ứng dụng (<c>x-bedrock-attempt</c>) thay vì parse <c>x-death</c>:</b> <c>x-death</c>
/// gộp count theo cặp (queue, reason) và có thể reset/di dời khi topology đổi → đếm không tin cậy để giới hạn retry.
/// App tự tăng header mỗi vòng ⇒ ngưỡng deterministic, kiểm chứng được.
/// </para>
/// <para>
/// <b>Vì sao retry publish dùng channel RIÊNG có publisher-confirms:</b> retry là APP-PUBLISH (khác dead-letter final
/// vốn broker-side qua NACK). Nếu ACK bản gốc TRƯỚC khi broker xác nhận đã nhận bản retry, một hiccup broker sẽ LÀM
/// MẤT message (phá at-least-once). Channel confirms bắt <c>PublishToRetryAsync</c> chờ tới khi broker ack; publish
/// lỗi → KHÔNG ack bản gốc mà NACK requeue=<c>false</c> → DLX final (bảo toàn, không rơi message). Channel tách khỏi
/// channel consume để không xen vào chuỗi delivery/ack.
/// </para>
/// </summary>
public sealed partial class RabbitMqConsumer(
    RabbitMqOptions options,
    RabbitMqConsumerOptions consumerOptions,
    RabbitMqConsumerState state,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;
    private readonly Lock _inFlightLock = new();
    private int _inFlight;
    private TaskCompletionSource _drained = CompletedDrain();

    // P0-02: channel RIÊNG cho retry-publish, bật publisher-confirms để await broker-ack trước khi ACK bản gốc
    // (bảo toàn at-least-once). Tách khỏi _channel (consume/ack) để không xen chuỗi delivery. Publish serialize qua gate
    // (IChannel không an toàn publish đồng thời; consumer dispatch có thể song song nếu prefetch > 1).
    private IChannel? _retryChannel;
    private readonly SemaphoreSlim _retryPublishGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        state.TransitionTo(RabbitMqConsumerStatus.Connecting);
        var factory = RabbitMqConnectionFactory.Create(options);
        try
        {
            _connection = await factory.CreateConnectionAsync(stoppingToken).ConfigureAwait(false);
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);

            await _channel.ExchangeDeclareAsync(
                exchange: options.ExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false,
                cancellationToken: stoppingToken).ConfigureAwait(false);

            await _channel.ExchangeDeclareAsync(
                exchange: consumerOptions.EffectiveDeadLetterExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken).ConfigureAwait(false);
            await _channel.QueueDeclareAsync(
                queue: consumerOptions.EffectiveDeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken).ConfigureAwait(false);
            await _channel.QueueBindAsync(
                queue: consumerOptions.EffectiveDeadLetterQueueName,
                exchange: consumerOptions.EffectiveDeadLetterExchangeName,
                routingKey: "#",
                cancellationToken: stoppingToken).ConfigureAwait(false);

            // P0-02 RETRY TOPOLOGY: retry exchange (topic, durable) + retry queue có x-message-ttl=RetryDelay và
            // x-dead-letter-exchange = MAIN exchange → message chờ hết TTL rồi tự dead-letter QUAY LẠI main (giữ routing
            // key gốc) → redeliver. App publish sang retry exchange (kèm attempt+1) cho lỗi transient còn lượt.
            await _channel.ExchangeDeclareAsync(
                exchange: consumerOptions.EffectiveRetryExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false,
                cancellationToken: stoppingToken).ConfigureAwait(false);
            var retryArguments = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["x-message-ttl"] = (int)consumerOptions.RetryDelay.TotalMilliseconds,
                ["x-dead-letter-exchange"] = options.ExchangeName, // hết TTL → quay lại main exchange.
            };
            await _channel.QueueDeclareAsync(
                queue: consumerOptions.EffectiveRetryQueueName, durable: true, exclusive: false, autoDelete: false,
                arguments: retryArguments,
                cancellationToken: stoppingToken).ConfigureAwait(false);
            await _channel.QueueBindAsync(
                queue: consumerOptions.EffectiveRetryQueueName,
                exchange: consumerOptions.EffectiveRetryExchangeName,
                routingKey: "#",
                cancellationToken: stoppingToken).ConfigureAwait(false);

            // Channel retry-publish riêng, BẬT publisher-confirms → BasicPublishAsync chờ broker-ack (bảo toàn khi retry).
            _retryChannel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                cancellationToken: stoppingToken).ConfigureAwait(false);

            var queueArguments = new Dictionary<string, object?>(consumerOptions.QueueArguments, StringComparer.Ordinal)
            {
                ["x-dead-letter-exchange"] = consumerOptions.EffectiveDeadLetterExchangeName,
            };

            await _channel.QueueDeclareAsync(
                queue: consumerOptions.QueueName, durable: true, exclusive: false, autoDelete: false,
                arguments: queueArguments,
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

            state.TransitionTo(RabbitMqConsumerStatus.TopologyReady);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnReceivedAsync;
            consumer.RegisteredAsync += OnConsumerRegisteredAsync;
            consumer.UnregisteredAsync += OnConsumerUnregisteredAsync;
            consumer.ShutdownAsync += OnConsumerShutdownAsync;

            _consumerTag = await _channel.BasicConsumeAsync(
                queue: consumerOptions.QueueName, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken).ConfigureAwait(false);

            state.TransitionTo(RabbitMqConsumerStatus.Consuming, _consumerTag);
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown while connecting/provisioning.
        }
        catch
        {
            state.TransitionTo(RabbitMqConsumerStatus.Faulted);
            throw;
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        BeginDelivery();
        try
        {
            var channel = _channel!;
            var deliveryTag = ea.DeliveryTag;

            // Envelope: MessageId (= IntegrationEvent.Id) + header event-type. Thiếu/hỏng → NACK requeue=false → DLX (quarantine).
            if (!Guid.TryParse(ea.BasicProperties.MessageId, out var messageId))
            {
                Log.MalformedEnvelope(logger, "MessageId", ea.BasicProperties.MessageId ?? "(null)");
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false).ConfigureAwait(false);
                return;
            }

            var eventType = DecodeHeader(ea.BasicProperties.Headers, RabbitMqMessageMapper.EventTypeHeader);
            if (string.IsNullOrEmpty(eventType))
            {
                Log.MalformedEnvelope(logger, RabbitMqMessageMapper.EventTypeHeader, "(null)");
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false).ConfigureAwait(false);
                return;
            }

            var incoming = new IncomingIntegrationMessage(
                messageId, consumerOptions.EffectiveConsumerName, eventType, ea.Body.ToArray())
            {
                // A-10: KHÔNG vứt metadata envelope — mang content-type (dispatcher validate == JSON) + schema-version.
                ContentType = ea.BasicProperties.ContentType,
                SchemaVersion = DecodeSchemaVersion(ea.BasicProperties.Headers),
                // P1-14: W3C trace context từ header CHUẨN traceparent/tracestate (giữ tracestate) → dispatcher dựng child span.
                TraceParent = DecodeHeader(ea.BasicProperties.Headers, RabbitMqMessageMapper.TraceParentHeader),
                TraceState = DecodeHeader(ea.BasicProperties.Headers, RabbitMqMessageMapper.TraceStateHeader),
                // Business correlation (tách trace) từ BasicProperties.CorrelationId theo đúng quy ước AMQP.
                CorrelationId = ea.BasicProperties.CorrelationId,
            };

            // P0-02: số lần đã giao TRƯỚC lần này (0 nếu lần đầu từ main). Dùng để giới hạn retry transient.
            var priorAttempts = DecodeAttempt(ea.BasicProperties.Headers);

#pragma warning disable CA1031 // Bắt rộng CÓ CHỦ ĐÍCH: lỗi handler/hạ tầng khi dispatch → phân loại transient/permanent (retry hoặc DLX), KHÔNG để ném ra dispatch loop của client (sẽ hạ consumer).
            DeliveryAction action;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = string.IsNullOrWhiteSpace(consumerOptions.DispatcherServiceKey)
                    ? scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>()
                    : scope.ServiceProvider.GetRequiredKeyedService<IIntegrationEventDispatcher>(
                        consumerOptions.DispatcherServiceKey);
                var outcome = await dispatcher.DispatchAsync(incoming).ConfigureAwait(false);
                action = RabbitMqDeliveryPolicy.ForOutcome(outcome);

                if (action == DeliveryAction.DeadLetter)
                {
                    Log.DeadLettered(logger, eventType, messageId);
                }
            }
            catch (Exception ex)
            {
                action = RabbitMqDeliveryPolicy.ForException(ex, priorAttempts, consumerOptions.MaxDeliveryAttempts);
                if (action == DeliveryAction.Retry)
                {
                    Log.RetryScheduled(logger, eventType, messageId, priorAttempts + 1, consumerOptions.MaxDeliveryAttempts, ex);
                }
                else
                {
                    Log.DispatchFailed(logger, eventType, messageId, priorAttempts + 1, ex);
                }
            }
#pragma warning restore CA1031

            await ApplyDeliveryActionAsync(channel, ea, deliveryTag, action, priorAttempts).ConfigureAwait(false);
        }
        finally
        {
            EndDelivery();
        }
    }

    /// <summary>
    /// Thực thi <see cref="DeliveryAction"/> đã quyết định. Retry: publish (CONFIRM) sang retry-exchange rồi ACK bản
    /// gốc; publish LỖI → NACK requeue=<c>false</c> → DLX final (KHÔNG ack → không mất message). DeadLetter/khác:
    /// NACK requeue=<c>false</c> (broker-side → DLX). Acknowledge: ACK.
    /// </summary>
    private async Task ApplyDeliveryActionAsync(
        IChannel channel, BasicDeliverEventArgs ea, ulong deliveryTag, DeliveryAction action, int priorAttempts)
    {
        switch (action)
        {
            case DeliveryAction.Acknowledge:
                await channel.BasicAckAsync(deliveryTag, multiple: false).ConfigureAwait(false);
                break;

            case DeliveryAction.Retry:
                var published = await TryPublishToRetryAsync(ea, priorAttempts + 1).ConfigureAwait(false);
                if (published)
                {
                    await channel.BasicAckAsync(deliveryTag, multiple: false).ConfigureAwait(false);
                }
                else
                {
                    // Không publish được bản retry → KHÔNG ack; đẩy về DLX final để không mất message.
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false).ConfigureAwait(false);
                }

                break;

            default: // DeadLetter
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Publish bản retry sang effective retry exchange (giữ routing key gốc), gắn header
    /// <c>x-bedrock-attempt</c> = <paramref name="attempt"/> và BẢO TOÀN envelope (MessageId/ContentType/event-type/
    /// schema-version/CorrelationId/Persistent). Channel bật confirms → await broker-ack. Trả <c>true</c> nếu broker
    /// xác nhận; <c>false</c> (nuốt exception có log) nếu publish lỗi để caller đẩy về DLX final (bảo toàn message).
    /// </summary>
    private async Task<bool> TryPublishToRetryAsync(BasicDeliverEventArgs ea, int attempt)
    {
        var source = ea.BasicProperties;
        var headers = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [RabbitMqConsumerOptions.AttemptHeader] = attempt,
        };

        // Bảo toàn event-type + schema-version (dispatcher cần) + P1-14 trace context (traceparent/tracestate) — copy nguyên từ header gốc nếu có.
        CopyHeader(source.Headers, headers, RabbitMqMessageMapper.EventTypeHeader);
        CopyHeader(source.Headers, headers, RabbitMqMessageMapper.SchemaVersionHeader);
        CopyHeader(source.Headers, headers, RabbitMqMessageMapper.TraceParentHeader);
        CopyHeader(source.Headers, headers, RabbitMqMessageMapper.TraceStateHeader);

        var properties = new BasicProperties
        {
            MessageId = source.MessageId,
            ContentType = source.ContentType,
            Persistent = true, // bền qua broker-restart, khớp at-least-once.
            Headers = headers,
        };
        if (!string.IsNullOrEmpty(source.CorrelationId))
        {
            properties.CorrelationId = source.CorrelationId;
        }

        await _retryPublishGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _retryChannel!.BasicPublishAsync(
                exchange: consumerOptions.EffectiveRetryExchangeName,
                routingKey: ea.RoutingKey, // = event-type gốc → retry-queue bind "#" nhận, dead-letter về main giữ key.
                mandatory: true,
                basicProperties: properties,
                body: ea.Body.ToArray()).ConfigureAwait(false);
            return true;
        }
#pragma warning disable CA1031 // Publish retry lỗi → trả false để caller NACK→DLX (bảo toàn), KHÔNG hạ consumer.
        catch (Exception ex)
        {
            Log.RetryPublishFailed(logger, ea.RoutingKey, source.MessageId ?? "(null)", ex);
            return false;
        }
#pragma warning restore CA1031
        finally
        {
            _retryPublishGate.Release();
        }
    }

    private static void CopyHeader(IDictionary<string, object?>? source, Dictionary<string, object?> target, string key)
    {
        if (source is not null && source.TryGetValue(key, out var value) && value is not null)
        {
            target[key] = value;
        }
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

    /// <summary>
    /// Đọc schema-version header (producer gắn int > 0). P1-02/AD-093: KHÔNG default 1 âm thầm — thiếu / không phân
    /// giải được / overflow / âm → trả 0 (SENTINEL INVALID) để dispatcher quarantine (producer hỏng/foreign không
    /// giả dạng v1). AMQP có thể trả int/long/short/byte hoặc byte[]/string.
    /// </summary>
    private static int DecodeSchemaVersion(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(RabbitMqMessageMapper.SchemaVersionHeader, out var value) || value is null)
        {
            return 0; // thiếu header → invalid (không default 1).
        }

        return value switch
        {
            int i => i,
            long l when l is >= int.MinValue and <= int.MaxValue => (int)l, // trong range int mới cast.
            long => 0,                                                      // overflow → invalid (không wrap).
            short s => s,
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0,                                                         // không phân giải được → invalid.
        };
    }

    /// <summary>
    /// Đọc header <c>x-bedrock-attempt</c> = số lần đã GIAO trước lần này (app tự tăng mỗi vòng retry). Thiếu/không
    /// phân giải/âm/overflow → 0 (coi như lần đầu — an toàn: chỉ khiến đếm lại từ đầu, không bao giờ bỏ qua giới hạn).
    /// </summary>
    private static int DecodeAttempt(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(RabbitMqConsumerOptions.AttemptHeader, out var value) || value is null)
        {
            return 0;
        }

        var decoded = value switch
        {
            int i => i,
            long l when l is >= int.MinValue and <= int.MaxValue => (int)l,
            long => 0,
            short s => s,
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0,
        };

        return decoded < 0 ? 0 : decoded;
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
    {
        var status = state.Snapshot().Status;
        if (status is not RabbitMqConsumerStatus.Stopping and not RabbitMqConsumerStatus.Stopped)
        {
            state.TransitionTo(RabbitMqConsumerStatus.Recovering);
        }

        return Task.CompletedTask;
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        var status = state.Snapshot().Status;
        if (status is not RabbitMqConsumerStatus.Stopping and not RabbitMqConsumerStatus.Stopped)
        {
            state.TransitionTo(RabbitMqConsumerStatus.Consuming, _consumerTag);
        }

        return Task.CompletedTask;
    }

    private Task OnConsumerRegisteredAsync(object sender, ConsumerEventArgs args)
    {
        var status = state.Snapshot().Status;
        if (status is not RabbitMqConsumerStatus.Stopping and not RabbitMqConsumerStatus.Stopped)
        {
            _consumerTag = args.ConsumerTags.FirstOrDefault() ?? _consumerTag;
            state.TransitionTo(RabbitMqConsumerStatus.Consuming, _consumerTag);
        }

        return Task.CompletedTask;
    }

    private Task OnConsumerUnregisteredAsync(object sender, ConsumerEventArgs args)
    {
        MarkRecoveringUnlessStopping();
        return Task.CompletedTask;
    }

    private Task OnConsumerShutdownAsync(object sender, ShutdownEventArgs args)
    {
        MarkRecoveringUnlessStopping();
        return Task.CompletedTask;
    }

    private void MarkRecoveringUnlessStopping()
    {
        var status = state.Snapshot().Status;
        if (status is not RabbitMqConsumerStatus.Stopping and not RabbitMqConsumerStatus.Stopped)
        {
            state.TransitionTo(RabbitMqConsumerStatus.Recovering);
        }
    }

    private void BeginDelivery()
    {
        lock (_inFlightLock)
        {
            if (_inFlight == 0)
            {
                _drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _inFlight++;
        }
    }

    private void EndDelivery()
    {
        lock (_inFlightLock)
        {
            _inFlight--;
            if (_inFlight == 0)
            {
                _drained.TrySetResult();
            }
        }
    }

    private Task WaitForDeliveriesAsync(CancellationToken cancellationToken)
    {
        lock (_inFlightLock)
        {
            return _drained.Task.WaitAsync(cancellationToken);
        }
    }

    private static TaskCompletionSource CompletedDrain()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        state.TransitionTo(RabbitMqConsumerStatus.Stopping);
        try
        {
            try
            {
                if (_channel?.IsOpen == true && !string.IsNullOrWhiteSpace(_consumerTag))
                {
                    await _channel.BasicCancelAsync(
                        _consumerTag,
                        noWait: false,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                await WaitForDeliveriesAsync(cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Broker may disconnect during cancel; execution loop still must be stopped below.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                Log.ShutdownFailed(logger, consumerOptions.QueueName, ex);
            }

            // Always signal BackgroundService, even when broker-side cancel/drain failed.
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Shutdown must still dispose channels when broker cancellation races with disconnect.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.ShutdownFailed(logger, consumerOptions.QueueName, ex);
        }
        finally
        {
            if (_retryChannel is not null)
            {
                await _retryChannel.DisposeAsync().ConfigureAwait(false);
            }

            if (_channel is not null)
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }

            _retryPublishGate.Dispose();
            state.TransitionTo(RabbitMqConsumerStatus.Stopped);
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
            Message = "Dispatch failed permanently (event-type={EventType}, messageId={MessageId}, attempt={Attempt}); NACK requeue=false → DLX.")]
        public static partial void DispatchFailed(ILogger logger, string eventType, Guid messageId, int attempt, Exception exception);

        [LoggerMessage(EventId = 4, Level = LogLevel.Warning,
            Message = "Malformed envelope (missing/invalid {Field}={Value}); NACK requeue=false → DLX.")]
        public static partial void MalformedEnvelope(ILogger logger, string field, string value);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
            Message = "Transient dispatch failure (event-type={EventType}, messageId={MessageId}); scheduling retry {Attempt}/{MaxAttempts}.")]
        public static partial void RetryScheduled(ILogger logger, string eventType, Guid messageId, int attempt, int maxAttempts, Exception exception);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error,
            Message = "Retry publish failed (routingKey={RoutingKey}, messageId={MessageId}); NACK requeue=false → DLX (no loss).")]
        public static partial void RetryPublishFailed(ILogger logger, string routingKey, string messageId, Exception exception);

        [LoggerMessage(EventId = 7, Level = LogLevel.Warning,
            Message = "RabbitMQ consumer shutdown encountered an error (queue={Queue}); resources will still be disposed.")]
        public static partial void ShutdownFailed(ILogger logger, string queue, Exception exception);
    }
}
