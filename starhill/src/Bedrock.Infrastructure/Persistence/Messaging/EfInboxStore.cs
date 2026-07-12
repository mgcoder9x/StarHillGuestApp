using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Impl <see cref="IInboxStore"/> (idempotency phía consumer — F30, design §7.3). STAGE bản ghi
/// <see cref="InboxMessage"/> vào ChangeTracker (không tự commit) → mark inbox + business của handler cùng
/// một <c>SaveChanges</c>/transaction. Trả <c>true</c> nếu lần đầu, <c>false</c> nếu đã xử lý.
/// <para>
/// Guard trùng cấp DB là PK <c>(message_id, consumer)</c>: hai consumer đồng thời → đúng một thắng ở commit
/// (kẻ thua nhận PK violation → NACK/redeliver → lần sau <see cref="TryMarkProcessedAsync"/> thấy đã tồn tại →
/// bỏ qua). Bản kiểm tra tồn tại ở đây chặn double-run trong luồng tuần tự thường gặp; tính nguyên tử dưới
/// tải đồng thời được kiểm chứng ở task 7.4 (Testcontainers).
/// </para>
/// </summary>
public sealed class EfInboxStore(PlatformDbContext context, IClock clock) : IInboxStore
{
    public async Task<bool> TryMarkProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);

        var alreadyProcessed = await context.Set<InboxMessage>()
            .AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct)
            .ConfigureAwait(false);
        if (alreadyProcessed)
        {
            return false;
        }

        context.Set<InboxMessage>().Add(new InboxMessage
        {
            MessageId = messageId,
            Consumer = consumer,
            ProcessedAt = clock.UtcNow,
        });
        return true;
    }
}
