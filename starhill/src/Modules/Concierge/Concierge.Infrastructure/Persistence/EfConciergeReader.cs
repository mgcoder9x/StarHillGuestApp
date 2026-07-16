using Concierge.Application;
using Concierge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concierge.Infrastructure.Persistence;

/// <summary>
/// Impl EF read-model <see cref="IConciergeReader"/> (no-tracking). <see cref="GetGuestConversationByVisitAsync"/>:
/// hội thoại của visit + tin sắp CŨ→MỚI theo Id (UUIDv7 time-ordered = CreatedAt asc nhưng PROVIDER-AGNOSTIC —
/// SQLite KHÔNG ORDER BY DateTimeOffset, bài học QR-N-059). <see cref="ListMessageIdsUnreadByGuestAsync"/>: id tin
/// của nhân viên khách chưa đọc. Mirror EfHousekeepingReader.
/// </summary>
public sealed class EfConciergeReader(ConciergeDbContext db) : IConciergeReader
{
    public async Task<GuestConversationView?> GetGuestConversationByVisitAsync(
        Guid guestVisitId, CancellationToken ct = default)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .Where(c => c.GuestVisitId == guestVisitId)
            .Select(c => new { c.Id, c.Status, c.LastMessageAt })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (conversation is null)
        {
            return null;
        }

        var messages = await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.Id)
            .Select(m => new GuestMessageView(m.Id, m.SenderType, m.Body, m.CreatedAt, m.ReadByStaffAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GuestConversationView(conversation.Id, conversation.Status, conversation.LastMessageAt, messages);
    }

    public async Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByGuestAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        return await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId
                        && m.SenderType == MessageSenderType.Staff
                        && m.ReadByGuestAt == null)
            .Select(m => m.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
