using GuestAccess.Application;
using GuestAccess.Domain;
using Microsoft.EntityFrameworkCore;

namespace GuestAccess.Infrastructure.Persistence;

/// <summary>
/// Impl EF của <see cref="IGuestSessionStore"/>. Row-lock (QR-AD-026): Npgsql chạy <c>SELECT ... FOR UPDATE</c>
/// (mirror precedent <c>EfOutboxDispatcher</c> — dùng <c>FromSqlRaw</c> + <c>ToListAsync</c> KHÔNG kèm operator để
/// EF gửi SQL nguyên trạng, không bọc subquery làm hỏng <c>FOR UPDATE</c>; tên bảng/schema lấy TỪ MODEL, không
/// hardcode → an toàn injection + không drift khi đổi tên). Provider khác (SQLite unit) → đọc thường (không lock).
/// Entity trả về TRACKED → mutate + SaveChanges persist trong cùng transaction do use case mở.
/// </summary>
public sealed class EfGuestSessionStore(GuestAccessDbContext db) : IGuestSessionStore
{
    public async Task<GuestSession?> FindByKeyHashForUpdateAsync(string sessionKeyHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionKeyHash);

        if (db.Database.IsNpgsql())
        {
            var sql = BuildForUpdateSql();
            var rows = await db.GuestSessions
                .FromSqlRaw(sql, sessionKeyHash)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return rows.Count > 0 ? rows[0] : null;
        }

        // Fallback provider-agnostic (SQLite test): không có FOR UPDATE — chỉ dùng cho test logic đơn luồng.
        return await db.GuestSessions
            .FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash, ct)
            .ConfigureAwait(false);
    }

    public void AddSession(GuestSession session) => db.GuestSessions.Add(session);

    public async Task<GuestVisit?> FindActiveVisitAsync(Guid guestSessionId, Guid roomId, CancellationToken ct = default) =>
        await db.GuestVisits
            .FirstOrDefaultAsync(
                v => v.GuestSessionId == guestSessionId && v.RoomId == roomId && v.Status == GuestVisitStatus.Active,
                ct)
            .ConfigureAwait(false);

    public void AddVisit(GuestVisit visit) => db.GuestVisits.Add(visit);

    /// <summary>
    /// Build <c>SELECT * FROM &lt;schema&gt;.&lt;table&gt; WHERE session_key_hash = {0} LIMIT 1 FOR UPDATE</c> cho
    /// Npgsql. Tên bảng/schema lấy từ model (khớp per-module schema + snake_case), quote an toàn; hash truyền qua
    /// tham số ({0}) — KHÔNG nội suy chuỗi (an toàn injection). <c>FOR UPDATE</c> đứng sau <c>LIMIT</c> (cú pháp Postgres).
    /// </summary>
    private string BuildForUpdateSql()
    {
        var entityType = db.Model.FindEntityType(typeof(GuestSession))
            ?? throw new InvalidOperationException("GuestSession chưa được map trong model.");
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException("GuestSession không có tên bảng.");
        var schema = entityType.GetSchema();
        var qualified = schema is null ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";

        return $"SELECT * FROM {qualified} WHERE session_key_hash = {{0}} LIMIT 1 FOR UPDATE";
    }

    private static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
