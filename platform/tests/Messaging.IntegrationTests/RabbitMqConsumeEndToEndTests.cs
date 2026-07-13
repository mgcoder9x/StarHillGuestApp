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

    private PostgreSqlContainer _postgres = null!;
    private RabbitMqContainer _rabbit = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (N-012). Root-cause N-067.
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            _rabbit = new RabbitMqBuilder("rabbitmq:3.13").Build();
            await _postgres.StartAsync().ConfigureAwait(false);
            await _rabbit.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker ⇒ skip.
        catch (Exception) when (!IsContinuousIntegration())
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

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

        // P0-02: số lần GIAO (kể cả lần lỗi) theo message id — dùng cho test retry tier.
        public ConcurrentDictionary<Guid, int> Deliveries { get; } = new();

        // P0-02: số lần handler ném transient (để khẳng định đã retry đúng số vòng).
        public int TransientFailures;

        /// <summary>Số lần giao TRANSIENT cần fail trước khi thành công (0 = luôn thành công).</summary>
        public int FailFirst { get; set; }
    }

    private sealed class MsgConsumeHandler(Recorder recorder) : IIntegrationEventHandler<MsgConsumeEvent>
    {
        public Task HandleAsync(MsgConsumeEvent integrationEvent, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            var delivery = recorder.Deliveries.AddOrUpdate(integrationEvent.Id, 1, (_, n) => n + 1);
            if (delivery <= recorder.FailFirst)
            {
                Interlocked.Increment(ref recorder.TransientFailures);
                // Lỗi TRANSIENT (không phải JsonException) → policy Retry (còn lượt) → publish retry → redeliver.
                throw new InvalidOperationException($"transient failure #{delivery} for {integrationEvent.Id}");
            }

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
        services.AddBedrockInbox<MsgTestDbContext>();
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

        // Start subscriber TRƯỚC → nó TỰ provision toàn bộ topology (main exchange + queue có arg
        // x-dead-letter-exchange + DLX + retry) và bind. KHÔNG pre-declare ở test: queue của consumer mang arg
        // x-dead-letter-exchange nên pre-declare không-arg sẽ 406 PRECONDITION_FAILED → consumer channel chết.
        var subscriber = provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>().Single();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await subscriber.StartAsync(cts.Token);
        try
        {
            await Task.Delay(1000); // chờ subscriber provision xong topology + binding rồi mới publish (không đua timing).

            // Publish qua đường THẬT (IEventBusPublisher = RabbitMqEventBusPublisher): set MessageId + header event-type.
            var messageId = Guid.CreateVersion7();
            var payload = JsonSerializer.Serialize(
                new MsgConsumeEvent(messageId, DateTimeOffset.UtcNow, "hello"), PayloadOptions);
            var publisher = provider.GetRequiredService<IEventBusPublisher>();
            await publisher.PublishAsync(new OutgoingIntegrationMessage
            {
                Id = messageId,
                EventType = EventType,
                SchemaVersion = 1,
                Payload = payload,
                OccurredAt = DateTimeOffset.UtcNow,
            });

            for (var attempt = 0; attempt < 300 && recorder.Handled.IsEmpty; attempt++)
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

    [SkippableFact]
    public async Task Transient_handler_failure_is_retried_with_delay_then_succeeds()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua retry end-to-end test.");

        const string retryExchange = "bedrock.events.retrytest";
        const string retryQueue = "retrytest.queue";

        var rabbitUri = new Uri(_rabbit.GetConnectionString());
        var rabbitUser = rabbitUri.UserInfo.Split(':', 2);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:HostName"] = rabbitUri.Host,
                ["RabbitMq:Port"] = rabbitUri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RabbitMq:UserName"] = Uri.UnescapeDataString(rabbitUser[0]),
                ["RabbitMq:Password"] = rabbitUser.Length > 1 ? Uri.UnescapeDataString(rabbitUser[1]) : string.Empty,
                ["RabbitMq:ExchangeName"] = retryExchange,
            })
            .Build();

        var recorder = new Recorder { FailFirst = 2 }; // fail 2 lần đầu (transient) → retry → thành công lần 3.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddBedrockInbox<MsgTestDbContext>();
        services.AddRabbitMqMessaging(configuration);
        services.AddIntegrationEventRegistry(typeof(MsgConsumeEvent).Assembly);
        services.AddIntegrationEventConsumer();
        services.AddSingleton(recorder);
        services.AddScoped<IIntegrationEventHandler<MsgConsumeEvent>, MsgConsumeHandler>();
        services.AddRabbitMqConsumer(o =>
        {
            o.QueueName = retryQueue;
            o.RoutingKeys.Add("msg.#");
            o.MaxDeliveryAttempts = 5;
            o.RetryDelay = TimeSpan.FromSeconds(1); // TTL nhỏ để test nhanh; vẫn chứng minh delay-requeue.
            o.RetryExchangeName = "bedrock.retrytest.retry";
            o.DeadLetterExchangeName = "bedrock.retrytest.dead-letter";
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MsgTestDbContext>().Database.EnsureCreatedAsync();
        }

        // Start subscriber TRƯỚC → nó tự provision toàn bộ topology (main+DLX+retry) và bind → tránh xung đột pre-declare.
        var subscriber = provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>().Single();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await subscriber.StartAsync(cts.Token);
        try
        {
            // Chờ subscriber sẵn sàng (exchange+queue+binding tồn tại) rồi mới publish → không mất message do đua timing.
            await Task.Delay(1000);

            var messageId = Guid.CreateVersion7();
            var payload = JsonSerializer.Serialize(
                new MsgConsumeEvent(messageId, DateTimeOffset.UtcNow, "retry"), PayloadOptions);
            var publisher = provider.GetRequiredService<IEventBusPublisher>();
            await publisher.PublishAsync(new OutgoingIntegrationMessage
            {
                Id = messageId,
                EventType = EventType,
                SchemaVersion = 1,
                Payload = payload,
                OccurredAt = DateTimeOffset.UtcNow,
            });

            // Poll tới khi handler thành công (sau 2 vòng retry, mỗi vòng ~ RetryDelay).
            for (var attempt = 0; attempt < 300 && recorder.Handled.IsEmpty; attempt++)
            {
                await Task.Delay(100);
            }

            Assert.Contains(messageId, recorder.Handled);           // cuối cùng xử lý thành công.
            Assert.Equal(2, recorder.TransientFailures);            // đúng 2 lần fail transient trước khi thành công.
            Assert.Equal(3, recorder.Deliveries[messageId]);        // giao 3 lần: fail, fail, success.

            // Inbox chỉ mark 1 lần (2 vòng đầu rollback không commit) → đúng-một-lần bất chấp redelivery.
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
