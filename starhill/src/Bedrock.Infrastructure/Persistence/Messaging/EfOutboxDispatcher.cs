using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Worker phát Outbox (design §7.2, R8.4–8.6, CP15). Một lượt <see cref="DispatchPendingAsync"/>:
/// mở transaction → claim batch pending (chưa processed, chưa dead-letter, tới hạn <c>next_attempt_at</c>)
/// → publish qua <see cref="IEventBusPublisher"/> → mark <c>processed_at</c>; fail → <c>error_count</c>++ +
/// backoff (<c>next_attempt_at</c>), vượt <c>MaxAttempts</c> → <c>dead_lettered_at</c> (cách ly poison, KHÔNG
/// chặn message khác) → SaveChanges + COMMIT (nhả claim).
/// <para>
/// <b>Generic theo <typeparamref name="TContext"/></b>: mỗi module đăng ký dispatcher riêng cho DbContext của
/// mình (per-module — bảng outbox nằm trong schema module, design §4.6) với named-options độc lập.
/// </para>
/// <para>
/// <b>At-least-once</b>: crash sau <c>PublishAsync</c> trước khi mark → publish lại lượt sau (Inbox phía consumer
/// khử trùng → hiệu ứng đúng-một-lần nghiệp vụ). <b>Claim exclusive đa-instance</b> (Postgres
/// <c>FOR UPDATE SKIP LOCKED</c>) + test claim đồng thời thuộc task 7.4 (Testcontainers) — bản này claim
/// provider-agnostic (đúng single-instance; đa-instance vẫn an toàn nghiệp vụ nhờ Inbox).
/// </para>
/// </summary>
public sealed class EfOutboxDispatcher<TContext>(
    TContext context,
    IEventBusPublisher publisher,
    IClock clock,
    IOptionsMonitor<OutboxDispatcherOptions> optionsMonitor) : IOutboxDispatcher
    where TContext : PlatformDbContext
{
    public async Task DispatchPendingAsync(CancellationToken ct = default)
    {
        var options = optionsMonitor.Get(OutboxDispatcherOptions.KeyFor<TContext>());

        // ExecutionStrategy: tương thích retry của Npgsql — transaction do CHÍNH strategy mở lại khi retry.
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var now = clock.UtcNow;
                var batch = await ClaimBatchAsync(now, options.BatchSize, ct).ConfigureAwait(false);

                foreach (var message in batch)
                {
                    await TryPublishAsync(message, options, ct).ConfigureAwait(false);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Claim batch pending tới hạn. Điều kiện <c>processed_at IS NULL AND dead_lettered_at IS NULL</c> luôn ở
    /// SQL (khớp partial index <c>ix_outbox_pending</c>). Với Npgsql (production): lọc <c>next_attempt_at</c> +
    /// <c>ORDER BY occurred_at</c> cũng ở SQL. Provider khác (SQLite test): EF không dịch được so sánh/sắp xếp
    /// <c>DateTimeOffset</c> → làm phần đó client-side (tập pending nhỏ vì dispatcher poll thường xuyên).
    /// </summary>
    private async Task<List<OutboxMessage>> ClaimBatchAsync(DateTimeOffset now, int batchSize, CancellationToken ct)
    {
        if (context.Database.IsNpgsql())
        {
            // CP15 (design §4.5, R8.6): claim NGUYÊN TỬ đa-instance bằng FOR UPDATE SKIP LOCKED — dispatcher A
            // khoá row nó lấy; dispatcher B BỎ QUA row đang khoá (không chờ, không claim trùng) → KHÔNG double-publish.
            // EF LINQ không dịch được SKIP LOCKED → raw SQL. Tên bảng/schema lấy TỪ MODEL (không hardcode, an toàn
            // injection; now/batchSize là tham số). Row trả về vẫn được ChangeTracker theo dõi → set ProcessedAt persist.
            var sql = BuildNpgsqlClaimSql();
            return await context.Set<OutboxMessage>()
                .FromSqlRaw(sql, now, batchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        var candidates = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.DeadLetteredAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return [.. candidates
            .Where(m => m.NextAttemptAt is null || m.NextAttemptAt <= now)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)];
    }

    /// <summary>
    /// Build câu SELECT ... FOR UPDATE SKIP LOCKED cho Npgsql. Tên bảng/schema lấy từ model (khớp per-module
    /// schema + snake_case), quote an toàn; điều kiện pending khớp partial index <c>ix_outbox_pending</c>.
    /// </summary>
    private string BuildNpgsqlClaimSql()
    {
        var entityType = context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage chưa được map (thiếu modelBuilder.AddOutboxInbox()?).");
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException("OutboxMessage không có tên bảng.");
        var schema = entityType.GetSchema();
        var qualified = schema is null ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";

        return "SELECT * FROM " + qualified
            + " WHERE processed_at IS NULL AND dead_lettered_at IS NULL"
            + " AND (next_attempt_at IS NULL OR next_attempt_at <= {0})"
            + " ORDER BY occurred_at LIMIT {1} FOR UPDATE SKIP LOCKED";
    }

    private static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private async Task TryPublishAsync(OutboxMessage message, OutboxDispatcherOptions options, CancellationToken ct)
    {
#pragma warning disable CA1031 // Publish tới bus ngoài có thể ném BẤT KỲ (network/serialize/adapter). Phải bắt rộng để backoff/dead-letter và cách ly poison — không rethrow để message khác trong batch vẫn xử lý (design §7.2).
        try
        {
            await publisher.PublishAsync(message, ct).ConfigureAwait(false);
            message.ProcessedAt = clock.UtcNow;
            OutboxMetrics.RecordPublished(message.ProcessedAt.Value - message.OccurredAt); // R24.3: published + publish-lag.
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            message.ErrorCount++;
            if (message.ErrorCount >= options.MaxAttempts)
            {
                message.DeadLetteredAt = clock.UtcNow; // cách ly, không retry tự động (R8.5/AD-016).
                OutboxMetrics.RecordDeadLettered(); // R24.3: dead-letter count.
            }
            else
            {
                var backoff = OutboxBackoff.ComputeBackoff(message.ErrorCount, options);

                // Jitter tất định theo Id (0..20%): trải tải chống thundering herd mà KHÔNG dùng RNG (test được).
                var jitterSeed = message.Id.GetHashCode() & int.MaxValue; // & MaxValue: tránh Math.Abs(int.MinValue) overflow.
                var jitterFraction = (jitterSeed % 1000) / 1000.0 * 0.2;
                message.NextAttemptAt = clock.UtcNow + backoff + (backoff * jitterFraction);
            }
        }
#pragma warning restore CA1031
    }
}
