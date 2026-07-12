using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Impl <see cref="IInboxStore"/> (idempotency phía consumer — F30, design §7.3). STAGE bản ghi
/// claim <see cref="InboxMessage"/> bằng INSERT atomic trong transaction hiện hành. Claim được flush TRƯỚC
/// handler nhưng chưa commit; handler lỗi → transaction rollback claim, delivery sau thử lại được.
/// <para>
/// Guard trùng cấp DB là PK <c>(message_id, consumer)</c>. PostgreSQL dùng <c>ON CONFLICT DO NOTHING</c>,
/// SQLite dùng <c>INSERT OR IGNORE</c>; vì arbitration xảy ra trước handler nên race không thể chạy handler hai lần.
/// </para>
/// </summary>
public sealed class EfInboxStore(PlatformDbContext context, IClock clock) : IInboxStore
{
    public async Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);

        var (table, schema) = ResolveTable();
        var qualified = schema is null ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";
        var insert = context.Database.IsNpgsql() ? "INSERT INTO " : "INSERT OR IGNORE INTO ";
        var conflict = context.Database.IsNpgsql() ? " ON CONFLICT DO NOTHING" : string.Empty;
        var sql = insert + qualified
            + " (message_id, consumer, processed_at) VALUES ({0}, {1}, {2})"
            + conflict;

        var affected = await context.Database
            .ExecuteSqlRawAsync(sql, [messageId, consumer, clock.UtcNow], ct)
            .ConfigureAwait(false);
        return affected == 1;
    }

    private (string Table, string? Schema) ResolveTable()
    {
        var entityType = context.Model.FindEntityType(typeof(InboxMessage))
            ?? throw new InvalidOperationException("InboxMessage chưa được map (thiếu AddOutboxInbox?).");
        return (
            entityType.GetTableName() ?? throw new InvalidOperationException("InboxMessage không có tên bảng."),
            entityType.GetSchema());
    }

    private static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
