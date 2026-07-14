using GuestAccess.Domain;

namespace GuestAccess.Application;

/// <summary>
/// Cổng truy cập aggregate GuestAccess (session + visit) trong CÙNG DbContext/transaction. Tách port riêng (thay
/// vì <c>IRepository&lt;T&gt;</c> chung) vì cần <b>row-lock có kiểm soát</b>: <see cref="FindByKeyHashForUpdateAsync"/>
/// khóa hàng session (Postgres <c>FOR UPDATE</c>) để SERIALIZE các resolve cùng thiết bị → loại race tạo visit
/// trùng TRƯỚC khi chạm partial unique (QR-AD-026). Impl EF ở Infrastructure (inject GuestAccessDbContext cụ thể);
/// entity trả về được ChangeTracker theo dõi → mutate + <c>IUnitOfWork.SaveChangesAsync</c> persist trong cùng transaction.
/// </summary>
public interface IGuestSessionStore
{
    /// <summary>
    /// Tìm session theo hash cookie và KHÓA hàng (Npgsql <c>FOR UPDATE</c> — chờ khoá, KHÔNG skip) để serialize
    /// resolve cùng thiết bị. Trả entity TRACKED (mutate được) hoặc <c>null</c> nếu chưa có. Provider không phải
    /// Npgsql (SQLite unit test) → đọc thường không lock (chỉ dùng cho test logic provider-agnostic).
    /// </summary>
    Task<GuestSession?> FindByKeyHashForUpdateAsync(string sessionKeyHash, CancellationToken ct = default);

    void AddSession(GuestSession session);

    /// <summary>Visit Active của (session, room) — TRACKED. Gọi TRONG session-row-lock nên không cần khóa riêng.</summary>
    Task<GuestVisit?> FindActiveVisitAsync(Guid guestSessionId, Guid roomId, CancellationToken ct = default);

    void AddVisit(GuestVisit visit);
}
