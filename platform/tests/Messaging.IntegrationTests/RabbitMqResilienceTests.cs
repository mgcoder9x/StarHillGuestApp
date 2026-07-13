using System.Collections.Concurrent;
using System.Text.Json;
using Adapters.Messaging.RabbitMq;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>
/// RESILIENCE (Testcontainers/RabbitMQ+Postgres THẬT — nâng "production-oriented" lên có bằng chứng nghịch cảnh,
/// review P1-04): (1) DLQ cô lập per-consumer — poison của consumer này KHÔNG lẫn sang DLQ consumer khác; (2)
/// graceful shutdown drain — delivery ĐANG xử lý (in-flight) được HOÀN TẤT khi StopAsync (không cắt ngang/mất).
/// Đây là test VALIDATE code recovery/drain/DLX đã có (AD-060/AD-095) dưới điều kiện thật; chỉ sửa production nếu
/// test lộ defect. Skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(MessagingIntegrationDefinition.Name)]
public sealed class RabbitMqResilienceTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private RabbitMqContainer _rabbit = null!;
    private bool _available;

    private static readonly JsonSerializerOptions PayloadOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (N-012/N-067).
            // (Bản cũ để .Build() ở field-initializer/constructor → ném NGOÀI try → test FAIL thay vì skip khi
            // máy không Docker. Đây là fix mirror RabbitMqConsumeEndToEndTests.)
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            _rabbit = new RabbitMqBuilder("rabbitmq:3.13").Build();
            await _postgres.StartAsync().ConfigureAwait(false);
            await _rabbit.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker ⇒ skip (nhưng CI có Docker → KHÔNG nuốt, fail-closed A-07/N-012).
        catch (Exception) when (!IsContinuousIntegration())
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

    private IConfiguration BuildConfig(string exchangeName)
    {
        var uri = new Uri(_rabbit.GetConnectionString());
        var user = uri.UserInfo.Split(':', 2);
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:HostName"] = uri.Host,
                ["RabbitMq:Port"] = uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["RabbitMq:UserName"] = Uri.UnescapeDataString(user[0]),
                ["RabbitMq:Password"] = user.Length > 1 ? Uri.UnescapeDataString(user[1]) : string.Empty,
                ["RabbitMq:ExchangeName"] = exchangeName,
            })
            .Build();
    }

    private static byte[] SerializeEvent(Guid id, string data) =>
        JsonSerializer.SerializeToUtf8Bytes(new MsgConsumeEvent(id, DateTimeOffset.UtcNow, data), PayloadOptions);

    // ── Test 1: DLQ isolation ────────────────────────────────────────────────
    [SkippableFact]
    public async Task Poison_from_one_consumer_lands_only_in_its_own_dead_letter_queue()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua DLQ-isolation test.");

        const string exchange = "reslo.events";
        var configuration = BuildConfig(exchange);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddBedrockInbox<MsgTestDbContext>();
        services.AddRabbitMqMessaging(configuration);
        // Registry KHÔNG chứa event-type poison → dispatcher trả DeadLettered → NACK requeue=false → DLX của CHÍNH queue.
        services.AddIntegrationEventRegistry(typeof(MsgConsumeEvent).Assembly);
        services.AddIntegrationEventConsumer();
        services.AddRabbitMqConsumer(o => { o.QueueName = "reslo.a"; o.RoutingKeys.Add("reslo.a.#"); });
        services.AddRabbitMqConsumer(o => { o.QueueName = "reslo.b"; o.RoutingKeys.Add("reslo.b.#"); });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MsgTestDbContext>().Database.EnsureCreatedAsync();
        }

        var consumers = provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>().ToList();
        Assert.Equal(2, consumers.Count);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        foreach (var c in consumers)
        {
            await c.StartAsync(cts.Token);
        }

        try
        {
            await Task.Delay(1500); // chờ cả hai consumer provision topology (main + DLX + DLQ + retry) + bind.

            var idA = Guid.CreateVersion7();
            var idB = Guid.CreateVersion7();
            var publisher = provider.GetRequiredService<IEventBusPublisher>();
            // EventType = routing key. "reslo.a.poison" chỉ khớp binding "reslo.a.#" (consumer A); tương tự B.
            // Cả hai KHÔNG có trong registry → DeadLettered → vào DLX riêng của queue.
            await publisher.PublishAsync(new OutgoingIntegrationMessage
            {
                Id = idA, EventType = "reslo.a.poison", SchemaVersion = 1,
                Payload = System.Text.Encoding.UTF8.GetString(SerializeEvent(idA, "a")), OccurredAt = DateTimeOffset.UtcNow,
            });
            await publisher.PublishAsync(new OutgoingIntegrationMessage
            {
                Id = idB, EventType = "reslo.b.poison", SchemaVersion = 1,
                Payload = System.Text.Encoding.UTF8.GetString(SerializeEvent(idB, "b")), OccurredAt = DateTimeOffset.UtcNow,
            });

            // Poll DLQ của từng queue (mặc định "{queue}.dead-letter"). Mỗi DLQ chỉ chứa poison CỦA NÓ.
            var uri = new Uri(_rabbit.GetConnectionString());
            await using var connection = await new ConnectionFactory { Uri = uri }.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            var inA = await PollDeadLetterAsync(channel, "reslo.a.dead-letter");
            var inB = await PollDeadLetterAsync(channel, "reslo.b.dead-letter");

            Assert.Equal(idA.ToString(), inA?.BasicProperties.MessageId); // A-poison vào ĐÚNG DLQ của A.
            Assert.Equal(idB.ToString(), inB?.BasicProperties.MessageId); // B-poison vào ĐÚNG DLQ của B.

            // Cô lập: DLQ của A KHÔNG còn message thứ hai (không lẫn B); tương tự B.
            Assert.Null(await channel.BasicGetAsync("reslo.a.dead-letter", autoAck: true));
            Assert.Null(await channel.BasicGetAsync("reslo.b.dead-letter", autoAck: true));
        }
        finally
        {
            foreach (var c in consumers)
            {
                await c.StopAsync(CancellationToken.None);
            }
        }
    }

    private static async Task<BasicGetResult?> PollDeadLetterAsync(IChannel channel, string queue)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var result = await channel.BasicGetAsync(queue, autoAck: true);
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(100);
        }

        return null;
    }

    // ── Test 2: graceful shutdown drains in-flight ───────────────────────────
    [SkippableFact]
    public async Task Graceful_shutdown_completes_in_flight_deliveries_without_loss()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua shutdown-drain test.");

        const string exchange = "resdrain.events";
        const int messageCount = 15;
        var configuration = BuildConfig(exchange);
        var recorder = new DrainRecorder { HandlerDelay = TimeSpan.FromMilliseconds(300) };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddBedrockInbox<MsgTestDbContext>();
        services.AddRabbitMqMessaging(configuration);
        services.AddIntegrationEventRegistry(typeof(MsgConsumeEvent).Assembly);
        services.AddIntegrationEventConsumer();
        services.AddSingleton(recorder);
        services.AddScoped<IIntegrationEventHandler<MsgConsumeEvent>, DrainHandler>();
        services.AddRabbitMqConsumer(o =>
        {
            o.QueueName = "resdrain.queue";
            o.RoutingKeys.Add("msg.#");
            o.PrefetchCount = messageCount + 5; // tất cả delivery in-flight cùng lúc → chứng minh drain trọn.
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MsgTestDbContext>().Database.EnsureCreatedAsync();
        }

        var subscriber = provider.GetServices<IHostedService>().OfType<RabbitMqConsumer>().Single();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await subscriber.StartAsync(cts.Token);

        await Task.Delay(1000); // chờ consumer ready.

        var ids = new List<Guid>();
        var publisher = provider.GetRequiredService<IEventBusPublisher>();
        for (var i = 0; i < messageCount; i++)
        {
            var id = Guid.CreateVersion7();
            ids.Add(id);
            await publisher.PublishAsync(new OutgoingIntegrationMessage
            {
                Id = id, EventType = "msg.consume_event", SchemaVersion = 1,
                Payload = System.Text.Encoding.UTF8.GetString(SerializeEvent(id, "drain")), OccurredAt = DateTimeOffset.UtcNow,
            });
        }

        // Chờ TẤT CẢ delivery đã BẮT ĐẦU handler (in-flight) — không chờ hoàn tất — rồi mới StopAsync giữa chừng.
        for (var attempt = 0; attempt < 100 && recorder.Started < messageCount; attempt++)
        {
            await Task.Delay(50);
        }

        Assert.Equal(messageCount, recorder.Started); // tất cả đã in-flight khi ta ra lệnh dừng.
        Assert.True(recorder.Completed < messageCount, "cần có delivery CHƯA hoàn tất lúc StopAsync để chứng minh drain (không cắt ngang).");

        // StopAsync GIỮA CHỪNG: graceful drain phải để mọi delivery in-flight HOÀN TẤT (không cắt ngang, không mất).
        await subscriber.StopAsync(CancellationToken.None);

        Assert.Equal(messageCount, recorder.Completed); // MỌI in-flight đã chạy xong khi StopAsync trả về.

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            var inboxCount = await db.Set<InboxMessage>().CountAsync(m => ids.Contains(m.MessageId));
            Assert.Equal(messageCount, inboxCount); // inbox mark commit cho TẤT CẢ → không mất, đúng-một-lần.
        }
    }

    private sealed class DrainRecorder
    {
        private int _started;
        private int _completed;

        public TimeSpan HandlerDelay { get; init; }
        public int Started => Volatile.Read(ref _started);
        public int Completed => Volatile.Read(ref _completed);

        public void MarkStarted() => Interlocked.Increment(ref _started);
        public void MarkCompleted() => Interlocked.Increment(ref _completed);
    }

    private sealed class DrainHandler(DrainRecorder recorder) : IIntegrationEventHandler<MsgConsumeEvent>
    {
        public async Task HandleAsync(MsgConsumeEvent integrationEvent, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            recorder.MarkStarted();
            await Task.Delay(recorder.HandlerDelay, CancellationToken.None).ConfigureAwait(false);
            recorder.MarkCompleted();
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
