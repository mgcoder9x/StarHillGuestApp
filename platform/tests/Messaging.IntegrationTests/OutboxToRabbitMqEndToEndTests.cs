using System.Text;
using System.Text.Json.Nodes;
using Bedrock.Application.Events;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Adapters.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Messaging.IntegrationTests;

/// <summary>
/// END-TO-END backbone event-driven (Postgres + RabbitMQ THẬT): chứng minh chuỗi
/// <c>Outbox (state DB) → EfOutboxDispatcher (claim SKIP LOCKED) → RabbitMqEventBusPublisher → RabbitMQ → consumer</c>
/// chạy TÍCH HỢP. Trước đó chỉ test riêng: dispatcher+publisher-giả (OutboxDispatcherTests), publisher+broker
/// (RabbitMqPublishIntegrationTests). Đây khoá phần "chúng chạy chung": dispatcher dùng adapter THẬT publish tới
/// broker THẬT + mark processed. Skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(MessagingIntegrationDefinition.Name)]
public sealed class OutboxToRabbitMqEndToEndTests : IAsyncLifetime
{
    private const string ExchangeName = "bedrock.events";
    private const string EventType = "msg.test_event";

    private PostgreSqlContainer _postgres = null!;
    private RabbitMqContainer _rabbit = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (N-012). Root-cause N-067.
            _postgres = new PostgreSqlBuilder("postgres:16-alpine@sha256:57c72fd2a128e416c7fcc499958864df5301e940bca0a56f58fddf30ffc07777").Build();
            _rabbit = new RabbitMqBuilder("rabbitmq:3.13@sha256:87178a0ee3e2f52980ba356d38646ed1056705ff2d5ff281f8965456eaa0c1e3").Build();
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
    public async Task Outbox_message_is_dispatched_through_real_rabbitmq_and_marked_processed()
    {
        Skip.IfNot(_available, "Docker/Postgres/RabbitMQ không khả dụng — bỏ qua end-to-end test.");

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
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddBedrockPersistence<MsgTestDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        services.AddBedrockOutbox<MsgTestDbContext>();
        services.AddRabbitMqMessaging(configuration);       // override default → publish RabbitMQ thật.
        services.AddOutboxDispatcher<MsgTestDbContext>();    // dispatcher dùng chính IEventBusPublisher trên.
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        // Consumer khai exchange + queue + binding TRƯỚC khi dispatch (topic exchange bỏ message nếu chưa có queue bound).
        var consumerFactory = new ConnectionFactory { Uri = rabbitUri };
        await using var consumerConnection = await consumerFactory.CreateConnectionAsync();
        await using var consumerChannel = await consumerConnection.CreateChannelAsync();
        await consumerChannel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await consumerChannel.QueueDeclareAsync();
        await consumerChannel.QueueBindAsync(queue.QueueName, ExchangeName, EventType);

        // Seed một OutboxMessage (state DB) + SaveChanges.
        var messageId = Guid.CreateVersion7();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                EventType = EventType,
                SchemaVersion = 1,
                Payload = "{\"hello\":\"world\"}",
                OccurredAt = DateTimeOffset.UtcNow,
                CorrelationId = "corr-e2e",
            });
            await db.SaveChangesAsync();
        }

        // Dispatch: claim (SKIP LOCKED) → publish RabbitMQ thật → mark processed.
        await using (var scope = provider.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
            await dispatcher.DispatchPendingAsync();
        }

        // Consumer NHẬN được message (chuỗi chạy thật đầu-cuối).
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
        // Payload lưu jsonb → Postgres NORMALIZE text (thêm space sau ':', có thể đổi thứ tự key) → so NGỮ NGHĨA
        // JSON, KHÔNG so byte (consumer là tolerant reader — design §9.1; N-059).
        var receivedJson = JsonNode.Parse(Encoding.UTF8.GetString(received!.Body.Span));
        var expectedJson = JsonNode.Parse("{\"hello\":\"world\"}");
        Assert.True(JsonNode.DeepEquals(receivedJson, expectedJson), "payload JSON ngữ nghĩa phải khớp");
        Assert.Equal(messageId.ToString(), received.BasicProperties.MessageId);

        // Outbox row được mark processed (không republish lượt sau).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MsgTestDbContext>();
            var stored = await db.Set<OutboxMessage>().SingleAsync(m => m.Id == messageId);
            Assert.NotNull(stored.ProcessedAt);
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

/// <summary>DbContext test tối giản map Outbox/Inbox (isNpgsql=true) — cho dispatcher end-to-end.</summary>
public sealed class MsgTestDbContext(
    DbContextOptions<MsgTestDbContext> options,
    IClock clock,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher)
    : PlatformDbContext(options, clock, currentUser, dispatcher)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxInbox(isNpgsql: true);
    }
}
