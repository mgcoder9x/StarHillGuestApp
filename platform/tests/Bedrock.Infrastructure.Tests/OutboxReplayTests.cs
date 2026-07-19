using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

public sealed class OutboxReplayTests
{
    [Fact]
    public async Task Dry_run_requires_selector_and_does_not_mutate_or_audit()
    {
        await using var harness = await CreateHarnessAsync();
        var messageId = await SeedDeadLetterAsync(harness, "test.replayable");
        await using var serviceScope = harness.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayService>();

        var result = await service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = Guid.CreateVersion7(),
            Actor = "operator-1",
            Reason = "verify poison payload before replay",
            MessageIds = [messageId],
            DryRun = true,
        });

        Assert.True(result.DryRun);
        Assert.Single(result.Candidates);
        Assert.Equal(0, result.ReplayedCount);

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.NotNull(await db.Set<OutboxMessage>().SingleAsync(message => message.Id == messageId));
        Assert.Empty(await db.Set<OutboxReplayAudit>().ToListAsync());
    }

    [Fact]
    public async Task Replay_requeues_only_dead_lettered_message_and_writes_audit()
    {
        await using var harness = await CreateHarnessAsync();
        var messageId = await SeedDeadLetterAsync(harness, "test.replayable");
        var operationId = Guid.CreateVersion7();
        await using var serviceScope = harness.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayService>();

        var result = await service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = operationId,
            Actor = "operator-1",
            Reason = "confirmed downstream fix",
            MessageIds = [messageId],
            DryRun = false,
        });

        Assert.False(result.DryRun);
        Assert.Equal(1, result.ReplayedCount);

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var message = await db.Set<OutboxMessage>().SingleAsync(item => item.Id == messageId);
        Assert.Null(message.DeadLetteredAt);
        Assert.Equal(0, message.ErrorCount);
        Assert.Equal(harness.Clock.UtcNow, message.NextAttemptAt);
        Assert.Contains("poison", message.LastError, StringComparison.Ordinal);

        var audit = await db.Set<OutboxReplayAudit>().SingleAsync();
        Assert.Equal(operationId, audit.OperationId);
        Assert.Equal(messageId, audit.MessageId);
        Assert.Equal("test.replayable", audit.EventType);
        Assert.Equal(4, audit.ErrorCount);
        Assert.Contains("poison", audit.LastError, StringComparison.Ordinal);

        var operation = await db.Set<OutboxReplayOperation>().SingleAsync();
        Assert.Equal(operationId, operation.OperationId);
        Assert.Equal("operator-1", operation.Actor);
        Assert.Equal("confirmed downstream fix", operation.Reason);
        Assert.Equal(1, operation.ReplayedCount);

        await using var auditScope = harness.CreateScope();
        var reader = auditScope.ServiceProvider.GetRequiredService<IOutboxReplayAuditReader>();
        var record = await reader.GetAsync(operationId);
        Assert.NotNull(record);
        Assert.Equal(operationId, record.OperationId);
        Assert.Equal("operator-1", record.Actor);
        Assert.Equal("confirmed downstream fix", record.Reason);
        Assert.Equal(1, record.ReplayedCount);
        var auditedMessage = Assert.Single(record.Messages);
        Assert.Equal(messageId, auditedMessage.MessageId);
        Assert.Equal("test.replayable", auditedMessage.EventType);
        Assert.Equal(4, auditedMessage.ErrorCount);
    }

    [Fact]
    public async Task Audit_lookup_returns_null_for_unknown_operation_and_rejects_empty_id()
    {
        await using var harness = await CreateHarnessAsync();
        await using var serviceScope = harness.CreateScope();
        var reader = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayAuditReader>();

        Assert.Null(await reader.GetAsync(Guid.CreateVersion7()));
        await Assert.ThrowsAsync<ArgumentException>(() => reader.GetAsync(Guid.Empty));
    }

    [Fact]
    public async Task Replay_fails_closed_when_selector_matches_more_than_max_messages()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDeadLetterAsync(harness, "test.replayable");
        await SeedDeadLetterAsync(harness, "test.replayable");
        await using var serviceScope = harness.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = Guid.CreateVersion7(),
            Actor = "operator-1",
            Reason = "bounded replay",
            EventType = "test.replayable",
            MaxMessages = 1,
            DryRun = true,
        }));
    }

    [Fact]
    public async Task Replay_rejects_unscoped_or_invalid_actor_requests()
    {
        await using var harness = await CreateHarnessAsync();
        await using var serviceScope = harness.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayService>();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = Guid.CreateVersion7(),
            Actor = "operator-1",
            Reason = "missing selector",
        }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = Guid.CreateVersion7(),
            Actor = " ",
            Reason = "selector",
            EventType = "test.replayable",
        }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = Guid.CreateVersion7(),
            Actor = "operator-1",
            Reason = "null selector collection",
            MessageIds = null!,
        }));
    }

    [Fact]
    public async Task Replay_operation_id_is_single_use_and_second_selector_is_not_mutated()
    {
        await using var harness = await CreateHarnessAsync();
        var firstMessageId = await SeedDeadLetterAsync(harness, "test.first");
        var secondMessageId = await SeedDeadLetterAsync(harness, "test.second");
        var operationId = Guid.CreateVersion7();
        await using var serviceScope = harness.CreateScope();
        var service = serviceScope.ServiceProvider.GetRequiredService<IOutboxReplayService>();

        await service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = operationId,
            Actor = "operator-1",
            Reason = "first bounded operation",
            MessageIds = [firstMessageId],
            DryRun = false,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new OutboxReplayRequest
        {
            OperationId = operationId,
            Actor = "operator-2",
            Reason = "must not reuse operation identity",
            MessageIds = [secondMessageId],
            DryRun = false,
        }));

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        Assert.Null((await db.Set<OutboxMessage>().SingleAsync(message => message.Id == firstMessageId)).DeadLetteredAt);
        Assert.NotNull((await db.Set<OutboxMessage>().SingleAsync(message => message.Id == secondMessageId)).DeadLetteredAt);
        Assert.Single(await db.Set<OutboxReplayOperation>().ToListAsync());
        Assert.Single(await db.Set<OutboxReplayAudit>().ToListAsync());
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync() =>
        await PersistenceHarness.CreateAsync(services => services.AddOutboxReplay<TestDbContext>());

    private static async Task<Guid> SeedDeadLetterAsync(PersistenceHarness harness, string eventType)
    {
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var message = new OutboxMessage
        {
            EventType = eventType,
            Payload = "{}",
            OccurredAt = harness.Clock.UtcNow.AddMinutes(-5),
            ErrorCount = 4,
            DeadLetteredAt = harness.Clock.UtcNow.AddMinutes(-1),
            LastError = "InvalidOperationException: poison payload",
            LastAttemptAt = harness.Clock.UtcNow.AddMinutes(-1),
        };
        db.Set<OutboxMessage>().Add(message);
        await db.SaveChangesAsync();
        return message.Id;
    }
}
