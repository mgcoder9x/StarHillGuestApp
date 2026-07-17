using Concierge.Contracts;
using Concierge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concierge.Infrastructure.Persistence;

/// <summary>
/// Impl EF <see cref="IConciergeStatsQuery"/> (no-tracking, đọc-đếm) cho Host Dashboard. OpenConversations =
/// Status=Open; UnreadConversations = số HỘI THOẠI có UnreadForStaff&gt;0 (đếm hội thoại, không phải tổng tin — Req 9.1).
/// </summary>
public sealed class EfConciergeStatsQuery(ConciergeDbContext db) : IConciergeStatsQuery
{
    public async Task<ConciergeStats> GetStatsAsync(Guid resortId, CancellationToken ct = default)
    {
        var open = await db.Conversations
            .AsNoTracking()
            .CountAsync(c => c.ResortId == resortId && c.Status == ConversationStatus.Open, ct)
            .ConfigureAwait(false);

        var unread = await db.Conversations
            .AsNoTracking()
            .CountAsync(c => c.ResortId == resortId && c.UnreadForStaff > 0, ct)
            .ConfigureAwait(false);

        return new ConciergeStats(open, unread);
    }
}
