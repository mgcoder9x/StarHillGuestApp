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

    public async Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByStaffAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        return await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId
                        && m.SenderType == MessageSenderType.Guest
                        && m.ReadByStaffAt == null)
            .Select(m => m.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedConversations> ListConversationsAsync(
        Guid resortId, ConversationStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        var query = db.Conversations
            .AsNoTracking()
            .Where(c => c.ResortId == resortId);
        if (status is not null)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        // Board sắp LastMessageAt DESC (mới-hoạt-động-nhất trước — Req 5.3). Chỉ Postgres (SQLite KHÔNG ORDER BY DateTimeOffset).
        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .ThenByDescending(c => c.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(c => new ConversationSummary(c.Id, c.RoomId, c.Status, c.LastMessageAt, c.UnreadForStaff))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedConversations(items, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<ConversationDetailView?> GetConversationAsync(
        Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new
            {
                c.Id,
                c.RoomId,
                c.GuestVisitId,
                c.Status,
                c.LastMessageAt,
                c.UnreadForStaff,
            })
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
            .Select(m => new StaffMessageView(
                m.Id, m.SenderType, m.SenderUserId, m.Body, m.CreatedAt, m.ReadByStaffAt, m.ReadByGuestAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ConversationDetailView(
            conversation.Id,
            conversation.RoomId,
            conversation.GuestVisitId,
            conversation.Status,
            conversation.LastMessageAt,
            conversation.UnreadForStaff,
            messages);
    }
}
