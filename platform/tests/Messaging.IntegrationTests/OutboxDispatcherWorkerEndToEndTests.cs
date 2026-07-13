using System.Text;
using System.Text.Json.Nodes;
using Adapters.Messaging.RabbitMq;
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
/// Chứng minh VỎ LÊN LỊCH (<see cref="OutboxDispatcherHostedService{TContext}"/>) tự phát: KHÁC
/// <see cref="OutboxToRabbitMqEndToEndTests"/> (gọi <c>DispatchPendingAsync</c> thủ công một lần), test này
/// đi ĐÚNG đường Host thật — <c>AddOutboxDispatcherWorker&lt;TContext&gt;()</c> → <c>AddHostedService</c> →
/// <c>BackgroundService.StartAsync</c> khởi động vòng poll nền → seed outbox → worker TỰ ĐỘNG claim+publish+mark
/// mà KHÔNG có lời gọi dispatch tường minh nào. Đây là guard cho AD-056 (worker opt-in phát tự động).
/// Skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(MessagingIntegrationDefinition.Name)]
public sealed class OutboxDispatcherWorkerEndToEndTests : IAsyncLifetime
{
    private const string ExchangeName = "bedrock.events.worker";
    private const string EventType = "msg.worker_test_event";

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
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker ⇒ skip (không fail suite).
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

    [SkippableFact]
    public async Task Hosted_worker_auto_dispatches_seeded_outbox_message_without_manual_call()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua worker end-to-end test.");

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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddBedrockOutbox<MsgTestDbContext>();
        services.AddRabbitMqMessaging(configuration);
        services.AddOutboxDispatcher<MsgTestDbContext>();
        // Đăng ký worker qua đường THẬT (opt-in) + poll nhanh để test không chờ lâu.
        services.AddOutboxDispatcherWorker<MsgTestDbContext>(o => o.PollInterval = TimeSpan.FromMilliseconds(200));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        // Consumer khai exchange + queue + binding TRƯỚC (topic exchange bỏ message nếu chưa có queue bound).
        var consumerFactory = new ConnectionFactory { Uri = rabbitUri };
        await using var consumerConnection = await consumerFactory.CreateConnectionAsync();
        await using var consumerChannel = await consumerConnection.CreateChannelAsync();
        await consumerChannel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await consumerChannel.QueueDeclareAsync();
        await consumerChannel.QueueBindAsync(queue.QueueName, ExchangeName, EventType);

        // Seed OutboxMessage (chưa dispatch) — KHÔNG gọi DispatchPendingAsync.
        var messageId = Guid.CreateVersion7();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventType = EventType,
                SchemaVersion = 1,
                Payload = "{\"worker\":\"auto\"}",
                OccurredAt = DateTimeOffset.UtcNow,
                CorrelationId = "corr-worker",
            });
            await db.SaveChangesAsync();
        }

        // Khởi động WORKER nền (BackgroundService.StartAsync → ExecuteAsync poll loop). KHÔNG dispatch thủ công.
        var worker = provider.GetServices<IHostedService>()
            .OfType<OutboxDispatcherHostedService<MsgTestDbContext>>()
            .Single();
        using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await worker.StartAsync(stopCts.Token);
        try
        {
            // Consumer NHẬN được message do WORKER tự phát (poll ~200ms → vài giây là dư).
            BasicGetResult? received = null;
            for (var attempt = 0; attempt < 100 && received is null; attempt++)
            {
                received = await consumerChannel.BasicGetAsync(queue.QueueName, autoAck: true);
                if (received is null)
                {
                    await Task.Delay(100);
                }
            }

            Assert.NotNull(received);
            var receivedJson = JsonNode.Parse(Encoding.UTF8.GetString(received!.Body.Span));
            var expectedJson = JsonNode.Parse("{\"worker\":\"auto\"}");
            Assert.True(JsonNode.DeepEquals(receivedJson, expectedJson), "payload JSON ngữ nghĩa phải khớp");
            Assert.Equal(messageId.ToString(), received.BasicProperties.MessageId);

            // Outbox row được mark processed. LƯU Ý: publish RabbitMQ nằm TRONG transaction dispatcher NHƯNG TRƯỚC
            // khi Postgres commit → consumer có thể nhận message TRƯỚC khi ProcessedAt commit xong (đúng at-least-once).
            // POLL trong khi WORKER CÒN CHẠY (không assert tức thì, không stop trước) → worker hoàn tất commit; tránh
            // race timing dưới tải.
            DateTimeOffset? processedAt = null;
            for (var attempt = 0; attempt < 100 && processedAt is null; attempt++)
            {
                await using (var scope = provider.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
                    processedAt = (await db.Set<OutboxMessage>().SingleAsync(m => m.Id == messageId)).ProcessedAt;
                }

                if (processedAt is null)
                {
                    await Task.Delay(100);
                }
            }

            Assert.NotNull(processedAt);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None); // shutdown êm (guard bất biến #3).
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
