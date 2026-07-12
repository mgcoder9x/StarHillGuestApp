using System.Collections.Concurrent;
using Bedrock.Application.Events;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Entities;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Infrastructure.Persistence.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL — task 7.4): kiểm các đảm bảo CHỈ đúng trên Postgres thật (đa-connection):
/// <list type="bullet">
///   <item><b>CP6</b> — state + outbox commit/rollback CÙNG transaction.</item>
///   <item><b>CP15</b> — 2 dispatcher đồng thời KHÔNG claim trùng (FOR UPDATE SKIP LOCKED) → không double-publish.</item>
///   <item><b>CP8</b> — inbox idempotent dưới tải đồng thời (PK <c>(message_id, consumer)</c> → đúng 1 thắng).</item>
/// </list>
/// Skip có điều kiện nếu môi trường thiếu Docker (N-012, KHÔNG xoá).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; private set; } = null!;

    public bool Available { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Build() validate Docker và NÉM nếu thiếu → phải nằm TRONG try để catch → skip (không fail fixture). Root-cause N-067.
            Container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await Container.StartAsync().ConfigureAwait(false);
            Available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: bất kỳ lỗi khởi động container ⇒ coi như thiếu Docker → skip (không fail suite).
        catch (Exception) when (!IsContinuousIntegration())
#pragma warning restore CA1031
        {
            Available = false;
        }
    }

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task DisposeAsync()
    {
        if (Available)
        {
            await Container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Collection dùng chung MỘT container Postgres cho mọi test integration (outbox/inbox + rotation race) → chỉ
/// spin 1 broker, các test trong collection chạy TUẦN TỰ (không nhiễu dữ liệu; mỗi test tự TRUNCATE reset).
/// </summary>
[CollectionDefinition(PostgresFixtureDefinition.Name)]
public sealed class PostgresFixtureDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres-integration";
}

/// <summary>Entity state tối giản (KHÔNG audit/soft-delete/concurrency) — cô lập test outbox/inbox khỏi mapping xmin.</summary>
public sealed class PgTestState : Entity
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>DbContext Postgres chỉ map state + outbox/inbox (isNpgsql=true → payload jsonb + claim SKIP LOCKED).</summary>
public sealed class PgOutboxDbContext(
    DbContextOptions<PgOutboxDbContext> options,
    IClock clock,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher)
    : PlatformDbContext(options, clock, currentUser, dispatcher)
{
    public DbSet<PgTestState> States => Set<PgTestState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxInbox(isNpgsql: true);
        modelBuilder.AddRefreshTokens(); // dùng chung cho rotation race test (task 8.3, CP7).
    }
}

[Collection(PostgresFixtureDefinition.Name)]
public sealed class PostgresOutboxInboxTests(PostgresFixture fixture)
{
    private sealed class ConcurrentPublisher : IEventBusPublisher
    {
        public ConcurrentBag<Guid> Ids { get; } = new();

        public Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default)
        {
            Ids.Add(message.Id);
            return Task.CompletedTask;
        }
    }

    private async Task<ServiceProvider> BuildAsync(
        IEventBusPublisher publisher,
        TestClock clock,
        Action<OutboxDispatcherOptions>? configure = null,
        Action<OutboxRetentionOptions>? configureRetention = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ICurrentUser>(new TestCurrentUser { UserId = Guid.CreateVersion7() });
        services.AddBedrockPersistence<PgOutboxDbContext>(o => o.UseNpgsql(fixture.Container.GetConnectionString()));
        services.AddSingleton(publisher);
        services.AddOutboxDispatcher<PgOutboxDbContext>(configure);
        services.AddOutboxRetention<PgOutboxDbContext>(configureRetention);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            // KHÔNG EnsureDeleted (không drop được DB đang mở — 55006). Tạo schema idempotent rồi TRUNCATE
            // để mỗi test bắt đầu sạch (container dùng chung; test trong 1 class chạy tuần tự).
            await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
            await db.Database
                .ExecuteSqlRawAsync("TRUNCATE TABLE outbox_message, inbox_message, states, refresh_token RESTART IDENTITY CASCADE")
                .ConfigureAwait(false);
        }

        return provider;
    }

    [SkippableFact]
    public async Task State_and_outbox_commit_and_rollback_together() // CP6
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(new ConcurrentPublisher(), clock);

        Guid committedStateId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            var state = new PgTestState { Name = "ok" };
            committedStateId = state.Id;
            await uow.ExecuteInTransactionAsync(async ct =>
            {
                db.States.Add(state);
                await outbox.EnqueueAsync(new ThingHappened(Guid.CreateVersion7(), clock.UtcNow, "n"), ct);
                await uow.SaveChangesAsync(ct);
                return 0;
            });
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            Assert.True(await db.States.AnyAsync(s => s.Id == committedStateId));
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync());
        }

        // Rollback: throw trong transaction → state MỚI + outbox MỚI đều KHÔNG persist.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    db.States.Add(new PgTestState { Name = "bad" });
                    await outbox.EnqueueAsync(new ThingHappened(Guid.CreateVersion7(), clock.UtcNow, "n2"), ct);
                    await uow.SaveChangesAsync(ct);
                    throw new InvalidOperationException("boom");
                }));
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            Assert.False(await db.States.AnyAsync(s => s.Name == "bad"));
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync()); // vẫn chỉ 1 (từ nhánh success)
        }
    }

    [SkippableFact]
    public async Task Concurrent_dispatchers_never_double_claim() // CP15
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var publisher = new ConcurrentPublisher();
        var clock = new TestClock();
        await using var provider = await BuildAsync(publisher, clock, o => o.BatchSize = 3);

        const int count = 20;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            for (var i = 0; i < count; i++)
            {
                db.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    EventType = "test.thing_happened",
                    Payload = "{}",
                    OccurredAt = clock.UtcNow.AddSeconds(i),
                });
            }

            await db.SaveChangesAsync();
        }

        async Task DrainAsync()
        {
            for (var i = 0; i < 100; i++)
            {
                await using var scope = provider.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
                await dispatcher.DispatchPendingAsync();
                var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
                var remaining = await db.Set<OutboxMessage>()
                    .AnyAsync(m => m.ProcessedAt == null && m.DeadLetteredAt == null);
                if (!remaining)
                {
                    break;
                }
            }
        }

        await Task.WhenAll(DrainAsync(), DrainAsync());

        Assert.Equal(count, publisher.Ids.Count);              // đủ 20 lần publish
        Assert.Equal(count, publisher.Ids.Distinct().Count()); // KHÔNG id nào publish 2 lần → SKIP LOCKED đúng (CP15)
    }

    [SkippableFact]
    public async Task Concurrent_inbox_mark_is_idempotent_at_db() // CP8
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(new ConcurrentPublisher(), clock);
        var messageId = Guid.CreateVersion7();

        async Task<bool> TryMarkAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            if (!await inbox.TryMarkProcessedAsync(messageId, "consumer-a"))
            {
                return false;
            }

            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false; // thua race: PK (message_id, consumer) trùng ở commit.
            }
        }

        var results = await Task.WhenAll(TryMarkAsync(), TryMarkAsync(), TryMarkAsync(), TryMarkAsync());

        Assert.Equal(1, results.Count(won => won)); // đúng MỘT thắng dưới tải đồng thời.

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            var rows = await db.Set<InboxMessage>()
                .CountAsync(m => m.MessageId == messageId && m.Consumer == "consumer-a");
            Assert.Equal(1, rows); // đúng MỘT bản ghi inbox (idempotent).
        }
    }

    [SkippableFact]
    public async Task Retention_purge_deletes_expired_processed_keeps_pending_and_dead_letter() // AD-047 nhánh Npgsql ExecuteDelete
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(
            new ConcurrentPublisher(),
            clock,
            configureRetention: o =>
            {
                o.ProcessedRetention = TimeSpan.FromDays(7);
                o.DeadLetterRetention = null; // giữ dead-letter vô thời hạn (mặc định).
            });
        var now = clock.UtcNow;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            db.Set<OutboxMessage>().AddRange(
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-30), ProcessedAt = now.AddDays(-10) },   // processed cũ → XOÁ
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-1), ProcessedAt = now.AddHours(-1) },     // processed mới → giữ
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-30) },                                    // pending → giữ
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-30), DeadLetteredAt = now.AddDays(-10) }); // dead-letter cũ → giữ (TTL null)
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var retention = scope.ServiceProvider.GetRequiredService<EfOutboxRetention<PgOutboxDbContext>>();
            Assert.Equal(1, await retention.PurgeAsync()); // chỉ processed-cũ bị xoá (nhánh ExecuteDeleteAsync).
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            Assert.Equal(3, await db.Set<OutboxMessage>().CountAsync()); // recent + pending + dead-letter còn nguyên.
            Assert.True(await db.Set<OutboxMessage>().AnyAsync(m => m.DeadLetteredAt != null)); // dead-letter KHÔNG bị xoá.
            Assert.True(await db.Set<OutboxMessage>().AnyAsync(m => m.ProcessedAt == null && m.DeadLetteredAt == null)); // pending còn.
        }
    }

    [SkippableFact]
    public async Task Retention_purge_deletes_expired_dead_letter_when_ttl_set() // AD-047 nhánh Npgsql dead-letter TTL
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(
            new ConcurrentPublisher(),
            clock,
            configureRetention: o =>
            {
                o.ProcessedRetention = TimeSpan.FromDays(7);
                o.DeadLetterRetention = TimeSpan.FromDays(7); // bật dọn dead-letter cũ.
            });
        var now = clock.UtcNow;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            db.Set<OutboxMessage>().AddRange(
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-30), DeadLetteredAt = now.AddDays(-10) }, // dead-letter cũ → XOÁ (TTL bật)
                new OutboxMessage { EventType = "e", Payload = "{}", OccurredAt = now.AddDays(-1), DeadLetteredAt = now.AddHours(-1) });  // dead-letter mới → giữ
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var retention = scope.ServiceProvider.GetRequiredService<EfOutboxRetention<PgOutboxDbContext>>();
            Assert.Equal(1, await retention.PurgeAsync()); // chỉ dead-letter-cũ bị xoá (nhánh ExecuteDeleteAsync thứ 2).
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync()); // dead-letter mới còn.
        }
    }
}
