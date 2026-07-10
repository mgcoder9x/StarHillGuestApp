using System.Collections.Concurrent;
using System.Text.Json;
using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>Event test cho consume end-to-end (registry quét assembly này → resolve theo EventType).</summary>
public sealed record MsgConsumeEvent(Guid Id, DateTimeOffset OccurredAt, string Data)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "msg.consume_event";
}

/// <summary>
/// END-TO-END nửa CONSUME (Postgres + RabbitMQ THẬT, AD-059/§7.3): publish message lên exchange → <see cref="RabbitMqConsumer"/>
/// (subscriber, đường THẬT AddRabbitMqConsumer→AddHostedService→BackgroundService) nhận → port
/// <see cref="IIntegrationEventDispatcher"/> (impl agnostic Infrastructure) → handler chạy + inbox mark. Chứng minh
/// transport + dispatch + idempotency chạy CHUNG trên artifact thật. Skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(MessagingIntegrationDefinition.Name)]
public sealed class RabbitMqConsumeEndToEndTests : IAsyncLifetime
{
    private const string ExchangeName = "bedrock.events.consume";
    private const string QueueName = "consumetest.queue";
    private const string EventType = "msg.consume_event";

    // Cache (CA1869): camelCase khớp OutboxSerialization.Options phía consumer deserialize.
    private static readonly JsonSerializerOptions PayloadOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13").Build();
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            await _postgres.StartAsync().ConfigureAwait(false);
            await _rabbit.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker ⇒ skip.
        catch (Exception)
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_available)
        {
            await _rabbit.DisposeAsync().ConfigureAwait(false);
            await _postgres.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class Recorder
    {
        public ConcurrentBag<Guid> Handled { get; } = [];
    }

    private sealed class MsgConsumeHandler(Recorder recorder) : IIntegrationEventHandler<MsgConsumeEvent>
    {
        public Task HandleAsync(MsgConsumeEvent integrationEvent, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            recorder.Handled.Add(integrationEvent.Id);
            return Task.CompletedTask;
        }
    }

    [SkippableFact]
    public async Task Published_message_is_consumed_by_subscriber_and_handler_runs_once()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua consume end-to-end test.");

        var rabbitUri = new Uri(_rabbit.GetConnectionString());
        var rabbitUser = rabbitUri.UserInfo.Split(':', 2);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:HostName"] = rabbitUri.Host,
                ["RabbitMq:Port"] = rabbitUri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RabbitMq:UserName"] = Uri.UnescapeDataString(rabbitUser[0]),
                ["RabbitMq:Password"] = rabbitUser.Length > 1 ? Uri.UnescapeDataString(rabbitUser[1]) : string.Empty,
                ["RabbitMq:ExchangeName"] = ExchangeName,
            })
            .Build();

        var recorder = new Recorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddRabbitMqMessaging(configuration);                 // publisher (để bơm message test) + kết nối.
        services.AddIntegrationEventRegistry(typeof(MsgConsumeEvent).Assembly);
        services.AddIntegrationEventConsumer();                        // core dispatch agnostic.
        services.AddSingleton(recorder);
        services.AddScoped<IIntegrationEventHandler<MsgConsumeEvent>, MsgConsumeHandler>();
        services.AddRabbitMqConsumer(o =>
        {
            o.QueueName = QueueName;
            o.RoutingKeys.Add("msg.#");
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MsgTestDbContext>().Database.EnsureCreatedAsync();
        }

        // Pre-declare exchange + queue + binding (khớp CHÍNH XÁC tham số subscriber → idempotent) TRƯỚC khi publish
        // → message được route vào queue chờ sẵn; subscriber start sau sẽ drain (tất định, không đua timing binding).
        await using (var connection = await new ConnectionFactory { Uri = rabbitUri }.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(QueueName, ExchangeName, "msg.#");
        }

        // Publish qua đường THẬT (IEventBusPublisher = RabbitMqEventBusPublisher): set MessageId + header event-type.
        var messageId = Guid.CreateVersion7();
        var payload = JsonSerializer.Serialize(
            new MsgConsumeEvent(messageId, DateTimeOffset.UtcNow, "hello"), PayloadOptions);
        var publisher = provider.GetRequiredService<IEventBusPublisher>();
        await publisher.PublishAsync(new OutboxMessage
        {
            Id = messageId,
            EventType = EventType,
            SchemaVersion = 1,
            Payload = payload,
            OccurredAt = DateTimeOffset.UtcNow,
        });

        // Start subscriber (BackgroundService) → drain queue → dispatch → handler + inbox.
        var subscriber = provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>().Single();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await subscriber.StartAsync(cts.Token);
        try
        {
            for (var attempt = 0; attempt < 100 && recorder.Handled.IsEmpty; attempt++)
            {
                await Task.Delay(100);
            }

            Assert.Single(recorder.Handled);
            Assert.Contains(messageId, recorder.Handled);

            // Inbox mark persist (idempotency) — đọc ở scope mới.
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            Assert.Equal(1, await db.Set<InboxMessage>().CountAsync(m => m.MessageId == messageId));
        }
        finally
        {
            await subscriber.StopAsync(CancellationToken.None);
        }
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }
}
