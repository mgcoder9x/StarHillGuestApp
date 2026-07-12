using System.Diagnostics;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 7.1/7.2 — outbox writer ghi CÙNG transaction với state (nền tảng CP6; race đa-connection thật để
/// Testcontainers task 7.4), serialize payload cố định, gắn correlation, mapping snake_case.
/// </summary>
public sealed class OutboxWriterTests
{
    [Fact]
    public async Task Enqueue_and_state_change_commit_atomically()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var result = await uow.ExecuteInTransactionAsync(async ct =>
            {
                repo.Add(new TestThing { Name = "x" });
                await writer.EnqueueAsync(
                    new ThingHappened(Guid.CreateVersion7(), DateTimeOffset.UtcNow, "x"), ct);
                await uow.SaveChangesAsync(ct);
                return 0;
            });

            Assert.Equal(0, result);
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(1, await db.Things.CountAsync());
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync()); // state + outbox cùng commit
        }
    }

    [Fact]
    public async Task Enqueue_rolls_back_with_state_on_error()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    repo.Add(new TestThing { Name = "x" });
                    await writer.EnqueueAsync(
                        new ThingHappened(Guid.CreateVersion7(), DateTimeOffset.UtcNow, "x"), ct);
                    await uow.SaveChangesAsync(ct);
                    throw new InvalidOperationException("boom");
                }));
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(0, await db.Things.IgnoreQueryFilters().CountAsync());
            Assert.Equal(0, await db.Set<OutboxMessage>().CountAsync()); // outbox rollback cùng state
        }
    }

    [Fact]
    public async Task Enqueue_serializes_payload_and_copies_event_fields()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var eventId = Guid.CreateVersion7();
        var occurredAt = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

        await using (var scope = harness.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await writer.EnqueueAsync(new ThingHappened(eventId, occurredAt, "hello"));
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await db.Set<OutboxMessage>().SingleAsync();

            Assert.Equal(eventId, message.Id); // Id event chảy xuyên suốt (AD-029)
            Assert.Equal("test.thing_happened", message.EventType);
            Assert.Equal(1, message.SchemaVersion);
            Assert.Equal(occurredAt, message.OccurredAt);
            Assert.Null(message.ProcessedAt);
            Assert.Equal(0, message.ErrorCount);
            // camelCase + giữ property của record dẫn xuất.
            Assert.Contains("\"name\":\"hello\"", message.Payload, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Enqueue_captures_correlation_id_from_current_activity()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        using var activity = new Activity("test-op").Start();
        var expected = activity.Id;
        Assert.NotNull(expected); // Start() luôn sinh Id (W3C)

        await using (var scope = harness.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await writer.EnqueueAsync(new ThingHappened(Guid.CreateVersion7(), DateTimeOffset.UtcNow, "x"));
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var message = await db.Set<OutboxMessage>().SingleAsync();
            Assert.Equal(expected, message.CorrelationId);
        }
    }

    [Fact]
    public async Task Outbox_inbox_tables_use_snake_case_and_expected_columns()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var outboxColumns = await GetColumnsAsync(db, "outbox_message");
        Assert.Contains("event_type", outboxColumns);
        Assert.Contains("schema_version", outboxColumns);
        Assert.Contains("next_attempt_at", outboxColumns);
        Assert.Contains("dead_lettered_at", outboxColumns);
        Assert.Contains("correlation_id", outboxColumns);

        var inboxColumns = await GetColumnsAsync(db, "inbox_message");
        Assert.Contains("message_id", inboxColumns);
        Assert.Contains("consumer", inboxColumns);
        Assert.Contains("processed_at", inboxColumns);
    }

    private static async Task<List<string>> GetColumnsAsync(DbContext db, string table)
    {
        var columns = new List<string>();
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }
}
