using Bedrock.Application.Messaging.Dispatch;
using Polly;
using RabbitMQ.Client;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Impl <see cref="IEventBusPublisher"/> bằng RabbitMQ (F29 — "cắm không sửa lõi"). Chỉ <c>IOutboxDispatcher</c>
/// (worker) gọi (CP11). Publish qua topic exchange (routing key = EventType) với publisher-confirms (await tới khi
/// broker xác nhận → reliable, khớp at-least-once của Outbox) + persistent message. Resilience (§9.2) bọc quanh
/// publish. Connection/channel dùng chung (lazy, tái tạo khi đóng); truy cập channel serialize qua semaphore
/// (IChannel không thread-safe cho publish đồng thời; dispatcher vốn publish tuần tự).
/// </summary>
public sealed class RabbitMqEventBusPublisher : IEventBusPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SemaphoreSlim _channelGate = new(1, 1);
    private readonly CreateChannelOptions _channelOptions =
        new(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqEventBusPublisher(RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _connectionFactory = RabbitMqConnectionFactory.Create(options);
        _resiliencePipeline = RabbitMqResiliencePipelineFactory.Create(options.Resilience);
    }

    public async Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // RabbitMQ IChannel không hỗ trợ concurrent publish. Gate bao TRỌN publish + confirm + retries,
        // không chỉ bao channel creation (A-04).
        await _channelGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _resiliencePipeline.ExecuteAsync(
                async token =>
                {
                    var channel = await GetOrCreateChannelUnderLockAsync(token).ConfigureAwait(false);
                    await channel.BasicPublishAsync(
                        exchange: _options.ExchangeName,
                        routingKey: RabbitMqMessageMapper.RoutingKeyOf(message),
                        mandatory: true,
                        basicProperties: RabbitMqMessageMapper.PropertiesOf(message),
                        body: RabbitMqMessageMapper.BodyOf(message),
                        cancellationToken: token).ConfigureAwait(false);
                },
                ct).ConfigureAwait(false);
        }
        finally
        {
            _channelGate.Release();
        }
    }

    /// <summary>Caller phải đang giữ <see cref="_channelGate"/>.</summary>
    private async Task<IChannel> GetOrCreateChannelUnderLockAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
            _channel = null;
        }

        if (_connection is not { IsOpen: true })
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }

            _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
        }

        _channel = await _connection.CreateChannelAsync(_channelOptions, ct).ConfigureAwait(false);
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct).ConfigureAwait(false);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _channelGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }
        }
        finally
        {
            _channelGate.Release();
            _channelGate.Dispose();
        }
    }
}
