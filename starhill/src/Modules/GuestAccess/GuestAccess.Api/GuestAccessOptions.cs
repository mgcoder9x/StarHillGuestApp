namespace GuestAccess.Api;

/// <summary>
/// Cấu hình cookie thiết bị khách (bind section "GuestAccess"; mặc định an toàn nếu thiếu config). Prefix
/// <c>__Host-</c> (QR-AD-025) buộc browser giữ Secure + Path=/ + không Domain → chống subdomain ghi đè cookie.
/// Lifetime 30–90 ngày (Req 11.2). ValidateOnStart chặn cấu hình sai ngay lúc boot (fail-fast).
/// </summary>
public sealed class GuestAccessOptions
{
    public const string SectionName = "GuestAccess";

    /// <summary>Tên cookie thiết bị — PHẢI bắt đầu <c>__Host-</c> để browser enforce Secure/Path=/.</summary>
    public string CookieName { get; set; } = "__Host-starhill_guest";

    /// <summary>Hạn cookie (ngày) — thiết bị dài hạn, trong [30,90] (Req 11.2).</summary>
    public int SessionCookieDays { get; set; } = 60;
}
