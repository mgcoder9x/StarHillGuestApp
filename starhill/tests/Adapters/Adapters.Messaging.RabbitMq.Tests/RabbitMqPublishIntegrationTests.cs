using System.Globalization;
using System.Text;
using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging.Dispatch;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Adapters.Messaging.RabbitMq.Tests;

/// <summary>
/// INTEGRATION (Testcontainers/RabbitMQ — F29, task 14): chứng minh <see cref="RabbitMqEventBusPublisher"/> publish
/// một <see cref="OutboxMessage"/> tới topic exchange thật và consumer bind theo routing key = EventType nhận lại
/// ĐÚNG payload + headers (adapter mỏng §5.2). Publisher-confirms bật → PublishAsync chỉ trả về khi broker đã nhận.
/// Skip có điều kiện nếu môi trường KHÔNG có Docker (N-012 — KHÔNG xoá, chạy khi có Docker/CI).
/// </summary>
public sealed class RabbitMqPublishIntegrationTests : IAsyncLifetime
{
    private const string ExchangeName = "bedrock.events";
    private const string EventType = "identity.user_token_refreshed";

    private RabbitMqContainer _container = null!;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (N-012). Root-cause N-067.
            _container = new RabbitMqBuilder("rabbitmq:3.13").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _dockerAvailable = true;
        }
#pragma warning disable CA1031 // CỐ Ý: bất kỳ lỗi khởi động container nào ⇒ coi như môi trường thiếu Docker → skip test (không fail suite).
        catch (Exception)
#pragma warning restore CA1031
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dockerAvailable)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [SkippableFact]
    public async Task Health_check_reports_healthy_when_broker_reachable()
    {
        Skip.IfNot(_dockerAvailable, "Docker/RabbitMQ container không khả dụng — bỏ qua integration test.");

        var uri = new Uri(_container.GetConnectionString());
        var userInfo = uri.UserInfo.Split(':', 2);
        var options = new RabbitMqOptions
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            ExchangeName = ExchangeName,
        };

        var healthCheck = new RabbitMqHealthCheck(options);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status); // broker sống → readiness Healthy (design §9.6/R34).
    }

    [SkippableFact]
    public async Task Published_message_is_routed_and_consumable_with_payload_and_headers()
    {
        Skip.IfNot(_dockerAvailable, "Docker/RabbitMQ container không khả dụng — bỏ qua integration test.");

        var uri = new Uri(_container.GetConnectionString());
        var userInfo = uri.UserInfo.Split(':', 2);
        var options = new RabbitMqOptions
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            ExchangeName = ExchangeName,
        };

        // Consumer khai exchange + queue + binding TRƯỚC khi publish (topic exchange bỏ message nếu chưa có queue bound).
        var consumerFactory = new ConnectionFactory { Uri = uri };
        await using var consumerConnection = await consumerFactory.CreateConnectionAsync();
        await using var consumerChannel = await consumerConnection.CreateChannelAsync();
        await consumerChannel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await consumerChannel.QueueDeclareAsync();
        await consumerChannel.QueueBindAsync(queue.QueueName, ExchangeName, EventType);

        var message = new OutboxMessage
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            EventType = EventType,
            SchemaVersion = 1,
            Payload = "{\"userId\":\"u-1\"}",
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = "corr-int-1",
        };

        await using (var publisher = new RabbitMqEventBusPublisher(options))
        {
            await publisher.PublishAsync(message);
        }

        // Poll queue tới khi nhận được message (broker định tuyến async).
        BasicGetResult? received = null;
        for (var attempt = 0; attempt < 50 && received is null; attempt++)
        {
            received = await consumerChannel.BasicGetAsync(queue.QueueName, autoAck: true);
            if (received is null)
            {
                await Task.Delay(100);
            }
        }

        Assert.NotNull(received);
        Assert.Equal(message.Payload, Encoding.UTF8.GetString(received!.Body.Span));
        Assert.Equal(message.Id.ToString(), received.BasicProperties.MessageId);

        var headers = received.BasicProperties.Headers;
        Assert.NotNull(headers);
        // Header string được AMQP truyền dạng byte[] (longstr) → decode để so.
        Assert.Equal(EventType, Encoding.UTF8.GetString((byte[])headers![RabbitMqMessageMapper.EventTypeHeader]!));
        Assert.Equal(1, Convert.ToInt32(headers[RabbitMqMessageMapper.SchemaVersionHeader], CultureInfo.InvariantCulture));
        Assert.Equal("corr-int-1", received.BasicProperties.CorrelationId);
    }
}
