namespace GuestAccess.Contracts;

/// <summary>
/// Hằng định danh module GuestAccess cho KEYED persistence (schema <c>guest_access</c>). Dùng khi
/// Host/Infrastructure gọi <c>AddBedrockPersistence&lt;GuestAccessDbContext&gt;(PersistenceKey, ...)</c> và khi
/// use case ghi khai <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của GuestAccess (chống
/// last-registration-wins P0-1). Nguồn duy nhất ở Contracts.
/// </summary>
public static class GuestAccessModule
{
    public const string PersistenceKey = "guest_access";

    /// <summary>
    /// Tên cookie thiết bị khách — HẰNG CANONICAL cross-module (QR-AD-025 + D-Rules.4c). GuestAccess phát/đọc cookie
    /// này; các module guest-facing khác (Rules/Faq/Concierge/Housekeeping) đọc cùng cookie qua hằng NÀY để phân giải
    /// ngữ cảnh (tránh mỗi module hardcode tên → drift). Prefix <c>__Host-</c> buộc browser giữ Secure+Path=/+no Domain.
    /// <c>GuestAccessOptions.CookieName</c> mặc định = hằng này (một nguồn sự thật).
    /// </summary>
    public const string SessionCookieName = "__Host-starhill_guest";
}
