namespace Concierge.Contracts;

/// <summary>Số liệu Concierge cho Dashboard vận hành (Req 9.1). DTO thuần (Contracts, không marker DI).</summary>
public sealed record ConciergeStats(int OpenConversations, int UnreadConversations);

/// <summary>
/// Query-port stats cross-module (QR-AD-002) cho Host Dashboard đọc số hội thoại mở + số hội thoại chưa đọc của một
/// resort. Impl EF đăng ký thủ công scoped ở Infrastructure. Chỉ ĐỌC-ĐẾM (no-tracking) — không repository/keyed.
/// </summary>
public interface IConciergeStatsQuery
{
    Task<ConciergeStats> GetStatsAsync(Guid resortId, CancellationToken ct = default);
}
