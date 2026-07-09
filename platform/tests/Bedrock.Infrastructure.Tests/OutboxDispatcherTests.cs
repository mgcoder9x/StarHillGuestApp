using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 7.3 — dispatcher outbox (publish/backoff/dead-letter, claim tới hạn), inbox idempotency, và type
/// registry. Claim exclusive đa-instance (Postgres skip-locked) + test đồng thời để task 7.4 (Testcontainers).
/// </summary>
public sealed class OutboxDispatcherTests
{
    [Fact]
    public async Task Dispatch_publishes_pending_and_marks_processed()
    {
        var publisher = new RecordingPublisher();
        await using var harness = await CreateHarnessAsync(publisher);
        var id = await SeedOutboxAsync(harness);

        await RunDispatcherAsync(harness);

        Assert.Single(publisher.Published);
        Assert.Equal(id, publisher.Published[0].Id);

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await db.Set<OutboxMessage>().SingleAsync();
        Assert.NotNull(stored.ProcessedAt);
        Assert.Equal(0, stored.ErrorCount);
    }

    [Fact]
    public async Task Dispatch_failure_increments_error_and_schedules_retry()
    {
        await using var harness = await CreateHarnessAsync(new FailingPublisher(), o => o.MaxAttempts = 5);
        await SeedOutboxAsync(harness);

        await RunDispatcherAsync(harness);

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await db.Set<OutboxMessage>().SingleAsync();
        Assert.Equal(1, stored.ErrorCount);
        Assert.Null(stored.ProcessedAt);
        Assert.Null(stored.DeadLetteredAt);
        Assert.NotNull(stored.NextAttemptAt);
        Assert.True(stored.NextAttemptAt > harness.Clock.UtcNow); // lùi sang tương lai (backoff).
    }

    [Fact]
    public async Task Dispatch_dead_letters_after_max_attempts()
    {
        await using var harness = await CreateHarnessAsync(new FailingPublisher(), o => o.MaxAttempts = 3);
        await SeedOutboxAsync(harness);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await RunDispatcherAsync(harness);
            harness.Clock.UtcNow = harness.Clock.UtcNow.AddHours(1); // vượt next_attempt_at để claim lại.
        }

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var stored = await db.Set<OutboxMessage>().SingleAsync();
        Assert.Equal(3, stored.ErrorCount);
        Assert.NotNull(stored.DeadLetteredAt);
        Assert.Null(stored.ProcessedAt);
    }

    [Fact]
    public async Task Dead_lettered_message_is_not_claimed_again()
    {
        var publisher = new RecordingPublisher();
        await using var harness = await CreateHarnessAsync(publisher);

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                EventType = "test.thing_happened",
                Payload = "{}",
                OccurredAt = harness.Clock.UtcNow,
                DeadLetteredAt = harness.Clock.UtcNow, // đã cách ly.
            });
            await db.SaveChangesAsync();
        }

        await RunDispatcherAsync(harness);

        Assert.Empty(publisher.Published); // dead-letter không được claim lại (CP: không auto-retry).
    }

    [Fact]
    public async Task Not_yet_due_message_is_not_claimed()
    {
        var publisher = new RecordingPublisher();
        await using var harness = await CreateHarnessAsync(publisher);

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            db.Set<OutboxMessage>().Add(new OutboxMessage
            {
                EventType = "test.thing_happened",
                Payload = "{}",
                OccurredAt = harness.Clock.UtcNow,
                NextAttemptAt = harness.Clock.UtcNow.AddHours(1), // chưa tới hạn.
            });
            await db.SaveChangesAsync();
        }

        await RunDispatcherAsync(harness);

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public void ComputeBackoff_is_exponential_and_capped()
    {
        var options = new OutboxDispatcherOptions
        {
            BaseDelay = TimeSpan.FromSeconds(5),
            MaxDelay = TimeSpan.FromMinutes(30),
        };

        Assert.Equal(TimeSpan.FromSeconds(5), OutboxBackoff.ComputeBackoff(1, options));
        Assert.Equal(TimeSpan.FromSeconds(10), OutboxBackoff.ComputeBackoff(2, options));
        Assert.Equal(TimeSpan.FromSeconds(20), OutboxBackoff.ComputeBackoff(3, options));
        Assert.Equal(TimeSpan.FromSeconds(40), OutboxBackoff.ComputeBackoff(4, options));
        Assert.Equal(TimeSpan.FromMinutes(30), OutboxBackoff.ComputeBackoff(20, options)); // capped.
    }

    [Fact]
    public async Task Inbox_marks_first_time_then_rejects_duplicate()
    {
        await using var harness = await CreateHarnessAsync(new RecordingPublisher());
        var messageId = Guid.CreateVersion7();

        await using (var scope = harness.CreateScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.True(await inbox.TryMarkProcessedAsync(messageId, "consumer-a"));
            await db.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            Assert.False(await inbox.TryMarkProcessedAsync(messageId, "consumer-a")); // đã xử lý.
        }

        await using (var scope = harness.CreateScope())
        {
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            Assert.True(await inbox.TryMarkProcessedAsync(messageId, "consumer-b")); // consumer khác = lần đầu.
        }
    }

    [Fact]
    public void Registry_resolves_known_eventtype_and_null_for_unknown()
    {
        var registry = new IntegrationEventTypeRegistry([typeof(ThingHappened).Assembly]);

        Assert.Equal(typeof(ThingHappened), registry.Resolve("test.thing_happened"));
        Assert.Null(registry.Resolve("does.not.exist"));
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync(
        IEventBusPublisher publisher,
        Action<OutboxDispatcherOptions>? configure = null) =>
        await PersistenceHarness.CreateAsync(services =>
        {
            services.AddSingleton(publisher);
            services.AddOutboxDispatcher<TestDbContext>(configure);
        });

    private static async Task<Guid> SeedOutboxAsync(PersistenceHarness harness)
    {
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var message = new OutboxMessage
        {
            EventType = "test.thing_happened",
            Payload = "{}",
            OccurredAt = harness.Clock.UtcNow,
        };
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }

    private static async Task RunDispatcherAsync(PersistenceHarness harness)
    {
        await using var scope = harness.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
        await dispatcher.DispatchPendingAsync();
    }
}

/// <summary>Publisher ghi lại message đã publish (thành công) — kiểm tra dispatcher gọi bus đúng.</summary>
public sealed class RecordingPublisher : IEventBusPublisher
{
    private readonly List<OutboxMessage> _published = [];

    public IReadOnlyList<OutboxMessage> Published => _published;

    public Task PublishAsync(OutboxMessage message, CancellationToken ct = default)
    {
        _published.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>Publisher luôn ném — kiểm tra nhánh backoff/dead-letter.</summary>
public sealed class FailingPublisher : IEventBusPublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken ct = default) =>
        throw new InvalidOperationException("bus tạm thời không khả dụng");
}
