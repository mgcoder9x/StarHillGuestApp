using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 7.5 — job retention outbox. Chứng minh <see cref="EfOutboxRetention{TContext}.PurgeAsync"/> chọn ĐÚNG
/// tập row hết hạn: xoá processed-cũ, KHÔNG xoá nhầm pending / processed-mới / dead-letter (R8.5). Test thứ hai
/// chứng minh khi bật <see cref="OutboxRetentionOptions.DeadLetterRetention"/> thì dead-letter cũ mới bị dọn.
/// </summary>
public sealed class OutboxRetentionTests
{
    // Mốc thời gian test cố định = TestClock.UtcNow (2026-07-08 12:00Z). Cutoff mặc định = now - 7 ngày.
    [Fact]
    public async Task Purge_removes_only_expired_processed_rows_keeping_pending_and_dead_letter()
    {
        await using var harness = await CreateHarnessAsync(); // default: ProcessedRetention 7d, DeadLetter null.
        var now = harness.Clock.UtcNow;

        var processedOld = NewMessage(m => m.ProcessedAt = now.AddDays(-8));   // quá hạn → XOÁ.
        var processedFresh = NewMessage(m => m.ProcessedAt = now.AddDays(-1)); // còn hạn → GIỮ.
        var pending = NewMessage(_ => { });                                    // chưa publish → GIỮ.
        var deadLetteredOld = NewMessage(m =>
        {
            m.ProcessedAt = now.AddDays(-30); // dù processed_at cũ, có dead_lettered_at → nhánh processed BỎ QUA.
            m.DeadLetteredAt = now.AddDays(-30);
        });

        await SeedAsync(harness, processedOld, processedFresh, pending, deadLetteredOld);

        var removed = await PurgeAsync(harness);

        Assert.Equal(1, removed);
        var remaining = await LoadIdsAsync(harness);
        Assert.DoesNotContain(processedOld.Id, remaining);
        Assert.Contains(processedFresh.Id, remaining);
        Assert.Contains(pending.Id, remaining);
        Assert.Contains(deadLetteredOld.Id, remaining); // dead-letter GIỮ khi DeadLetterRetention = null.
    }

    [Fact]
    public async Task Purge_removes_expired_dead_letter_when_retention_configured()
    {
        await using var harness = await CreateHarnessAsync(o =>
        {
            o.ProcessedRetention = TimeSpan.FromDays(7);
            o.DeadLetterRetention = TimeSpan.FromDays(14); // bật dọn dead-letter cũ hơn 14 ngày.
        });
        var now = harness.Clock.UtcNow;

        var deadLetteredOld = NewMessage(m =>
        {
            m.ProcessedAt = now.AddDays(-30);
            m.DeadLetteredAt = now.AddDays(-30); // quá 14 ngày → XOÁ.
        });
        var deadLetteredFresh = NewMessage(m =>
        {
            m.ProcessedAt = now.AddDays(-3);
            m.DeadLetteredAt = now.AddDays(-3); // trong 14 ngày → GIỮ.
        });
        var pending = NewMessage(_ => { }); // GIỮ.

        await SeedAsync(harness, deadLetteredOld, deadLetteredFresh, pending);

        var removed = await PurgeAsync(harness);

        Assert.Equal(1, removed);
        var remaining = await LoadIdsAsync(harness);
        Assert.DoesNotContain(deadLetteredOld.Id, remaining);
        Assert.Contains(deadLetteredFresh.Id, remaining);
        Assert.Contains(pending.Id, remaining);
    }

    [Fact]
    public async Task Purge_returns_zero_when_nothing_expired()
    {
        await using var harness = await CreateHarnessAsync();
        var now = harness.Clock.UtcNow;
        await SeedAsync(harness, NewMessage(m => m.ProcessedAt = now.AddDays(-1)), NewMessage(_ => { }));

        var removed = await PurgeAsync(harness);

        Assert.Equal(0, removed);
        Assert.Equal(2, (await LoadIdsAsync(harness)).Count);
    }

    private static OutboxMessage NewMessage(Action<OutboxMessage> configure)
    {
        var message = new OutboxMessage
        {
            EventType = "test.thing_happened",
            Payload = "{}",
            OccurredAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        configure(message);
        return message;
    }

    private static async Task<PersistenceHarness> CreateHarnessAsync(Action<OutboxRetentionOptions>? configure = null) =>
        await PersistenceHarness.CreateAsync(services => services.AddOutboxRetention<TestDbContext>(configure));

    private static async Task SeedAsync(PersistenceHarness harness, params OutboxMessage[] messages)
    {
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Set<OutboxMessage>().AddRange(messages);
        await db.SaveChangesAsync();
    }

    private static async Task<int> PurgeAsync(PersistenceHarness harness)
    {
        await using var scope = harness.CreateScope();
        var retention = scope.ServiceProvider.GetRequiredService<EfOutboxRetention<TestDbContext>>();
        return await retention.PurgeAsync();
    }

    private static async Task<List<Guid>> LoadIdsAsync(PersistenceHarness harness)
    {
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        return await db.Set<OutboxMessage>().Select(m => m.Id).ToListAsync();
    }
}
