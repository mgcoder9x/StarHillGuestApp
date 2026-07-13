using System.Collections.Concurrent;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>Integration event test cho nửa CONSUME (registry quét assembly test → resolve theo EventType).</summary>
public sealed record ConsumeTestEvent(Guid Id, DateTimeOffset OccurredAt, string Data)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "test.consume_event";
}

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL): core consume <see cref="EfIntegrationEventDispatcher"/> (AD-059, §7.3) —
/// đúng-một-lần (inbox), idempotent khi redeliver, và unknown EventType → dead-letter (không crash). Chạy trên
/// Postgres thật (inbox là bảng thật + transaction thật). Cùng collection fixture (1 container, tuần tự).
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class PostgresIntegrationEventDispatcherTests(PostgresFixture fixture)
{
    private const string Consumer = "consumer-x";

    private sealed class Recorder
    {
        public ConcurrentBag<Guid> Handled { get; } = [];
    }

    private sealed class ConsumeTestHandler(Recorder recorder) : IIntegrationEventHandler<ConsumeTestEvent>
    {
        public Task HandleAsync(ConsumeTestEvent integrationEvent, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(integrationEvent);
            recorder.Handled.Add(integrationEvent.Id);
            return Task.CompletedTask;
        }
    }

    private async Task<(ServiceProvider Provider, Recorder Recorder)> BuildAsync()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new TestClock());
        services.AddSingleton<ICurrentUser>(new TestCurrentUser { UserId = Guid.CreateVersion7() });
        services.AddBedrockPersistence<PgOutboxDbContext>(o => o.UseNpgsql(fixture.Container.GetConnectionString()));
        services.AddBedrockInbox<PgOutboxDbContext>();
        services.AddIntegrationEventRegistry(typeof(ConsumeTestEvent).Assembly);
        services.AddIntegrationEventConsumer();
        var recorder = new Recorder();
        services.AddSingleton(recorder);
        services.AddScoped<IIntegrationEventHandler<ConsumeTestEvent>, ConsumeTestHandler>();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
            await db.Database
                .ExecuteSqlRawAsync("TRUNCATE TABLE outbox_message, inbox_message, states, refresh_token RESTART IDENTITY CASCADE")
                .ConfigureAwait(false);
        }

        return (provider, recorder);
    }

    private static IncomingIntegrationMessage Message(Guid id, string eventType, string data)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new ConsumeTestEvent(id, DateTimeOffset.UtcNow, data), OutboxSerialization.Options);
        return new IncomingIntegrationMessage(id, Consumer, eventType, payload)
        {
            ContentType = "application/json",
        };
    }

    private static async Task<InboxDispatchOutcome> DispatchAsync(ServiceProvider provider, IncomingIntegrationMessage message)
    {
        // Scope MỖI message (như subscriber thật) → dispatcher/handler/inbox chung một PlatformDbContext.
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
        return await dispatcher.DispatchAsync(message).ConfigureAwait(false);
    }

    [SkippableFact]
    public async Task First_delivery_handles_once_and_records_inbox()
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var (provider, recorder) = await BuildAsync();
        await using var _ = provider;
        var id = Guid.CreateVersion7();

        var outcome = await DispatchAsync(provider, Message(id, "test.consume_event", "hello"));

        Assert.Equal(InboxDispatchOutcome.Handled, outcome);
        Assert.Single(recorder.Handled);
        Assert.Contains(id, recorder.Handled);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
        Assert.Equal(1, await db.Set<InboxMessage>().CountAsync(m => m.MessageId == id && m.Consumer == Consumer));
    }

    [SkippableFact]
    public async Task Redelivery_is_idempotent_handler_runs_once()
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var (provider, recorder) = await BuildAsync();
        await using var _ = provider;
        var id = Guid.CreateVersion7();

        var first = await DispatchAsync(provider, Message(id, "test.consume_event", "hello"));
        var second = await DispatchAsync(provider, Message(id, "test.consume_event", "hello")); // redeliver cùng messageId

        Assert.Equal(InboxDispatchOutcome.Handled, first);
        Assert.Equal(InboxDispatchOutcome.Duplicate, second);
        Assert.Single(recorder.Handled); // handler chạy ĐÚNG một lần dù giao 2 lần.
    }

    [SkippableFact]
    public async Task Unknown_event_type_is_dead_lettered_without_inbox_or_handler()
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var (provider, recorder) = await BuildAsync();
        await using var _ = provider;
        var id = Guid.CreateVersion7();

        var outcome = await DispatchAsync(provider, Message(id, "unknown.event_type", "x"));

        Assert.Equal(InboxDispatchOutcome.DeadLettered, outcome);
        Assert.Empty(recorder.Handled);                       // handler KHÔNG chạy.

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
        Assert.Equal(0, await db.Set<InboxMessage>().CountAsync()); // KHÔNG ghi inbox cho type lạ.
    }
}
