using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Job dọn (retention) bảng <c>outbox_message</c> (task 7.5, R8.5). Một lượt <see cref="PurgeAsync"/> XOÁ:
/// (1) row đã publish (<c>processed_at != null</c>, KHÔNG dead-letter) quá <see cref="OutboxRetentionOptions.ProcessedRetention"/>;
/// (2) row dead-letter quá <see cref="OutboxRetentionOptions.DeadLetterRetention"/> — CHỈ khi option này được set
/// (mặc định <c>null</c> = giữ vô thời hạn). Trả về tổng số row đã xoá.
/// <para>
/// <b>An toàn theo thiết kế</b>: điều kiện xoá processed luôn kèm <c>dead_lettered_at == null</c> → KHÔNG bao giờ
/// xoá nhầm row dead-letter theo nhánh processed; row pending (<c>processed_at == null</c>) không bao giờ khớp →
/// KHÔNG mất message chưa publish. Bản chất: giữ nguyên bất biến "chỉ dọn cái đã hoàn tất / đã cách ly có chủ đích".
/// </para>
/// <para>
/// <b>Generic theo <typeparamref name="TContext"/></b> + named-options: per-module như <see cref="EfOutboxDispatcher{TContext}"/>.
/// Base chỉ cung cấp LOGIC; Host lên lịch chạy (nhất quán với dispatcher — base KHÔNG có hosted-service).
/// </para>
/// <para>
/// <b>Provider-conditional</b> (giống dispatcher): Npgsql (production) xoá theo tập bằng
/// <c>ExecuteDeleteAsync</c> (set-based, 1 câu DELETE, không nạp entity). Provider khác (SQLite test) không dịch
/// được so sánh <c>DateTimeOffset</c> (cùng lý do DV-010) → nạp client-side, lọc, <c>RemoveRange</c> + <c>SaveChanges</c>.
/// </para>
/// </summary>
public sealed class EfOutboxRetention<TContext>(
    TContext context,
    IClock clock,
    IOptionsMonitor<OutboxRetentionOptions> optionsMonitor)
    where TContext : PlatformDbContext
{
    /// <summary>Chạy một lượt dọn. Trả về tổng số row <c>outbox_message</c> đã xoá.</summary>
    public async Task<int> PurgeAsync(CancellationToken ct = default)
    {
        var options = optionsMonitor.Get(OutboxRetentionOptions.KeyFor<TContext>());
        var now = clock.UtcNow;
        var processedCutoff = now - options.ProcessedRetention;
        var deadLetterCutoff = options.DeadLetterRetention is { } ttl ? now - ttl : (DateTimeOffset?)null;

        var set = context.Set<OutboxMessage>();

        if (context.Database.IsNpgsql())
        {
            // Set-based DELETE (không nạp entity). Hai câu tách biệt để điều kiện rõ ràng, đúng partial index.
            var removed = await set
                .Where(m => m.ProcessedAt != null && m.DeadLetteredAt == null && m.ProcessedAt < processedCutoff)
                .ExecuteDeleteAsync(ct)
                .ConfigureAwait(false);

            if (deadLetterCutoff is { } deadCutoff)
            {
                removed += await set
                    .Where(m => m.DeadLetteredAt != null && m.DeadLetteredAt < deadCutoff)
                    .ExecuteDeleteAsync(ct)
                    .ConfigureAwait(false);
            }

            return removed;
        }

        // Provider không dịch được so sánh DateTimeOffset (SQLite test): lọc client-side rồi xoá theo tập.
        var candidates = await set.ToListAsync(ct).ConfigureAwait(false);
        var expired = SelectExpired(candidates, processedCutoff, deadLetterCutoff);
        if (expired.Count == 0)
        {
            return 0;
        }

        set.RemoveRange(expired);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return expired.Count;
    }

    /// <summary>
    /// Chọn đúng tập row hết hạn (CA1859: trả concrete <see cref="List{T}"/>). Cùng vị từ với nhánh SQL để
    /// hành vi hai provider trùng khớp: processed-cũ-không-dead-letter, và dead-letter-cũ khi có TTL.
    /// </summary>
    private static List<OutboxMessage> SelectExpired(
        List<OutboxMessage> candidates,
        DateTimeOffset processedCutoff,
        DateTimeOffset? deadLetterCutoff)
    {
        var expired = new List<OutboxMessage>();
        foreach (var m in candidates)
        {
            var processedExpired = m.ProcessedAt is { } processed && m.DeadLetteredAt is null && processed < processedCutoff;
            var deadLetterExpired = deadLetterCutoff is { } deadCutoff
                && m.DeadLetteredAt is { } deadLettered && deadLettered < deadCutoff;

            if (processedExpired || deadLetterExpired)
            {
                expired.Add(m);
            }
        }

        return expired;
    }
}
