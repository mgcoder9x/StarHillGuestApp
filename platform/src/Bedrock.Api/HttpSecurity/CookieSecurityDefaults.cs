using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Cờ cookie an toàn theo <see cref="CookieSameSiteMode"/> (F17) — module set cookie dùng helper này để nhất quán.
/// Same-site → <c>SameSite=Lax</c> (không cần CSRF token); cross-site → <c>SameSite=None</c> (bắt buộc Secure) +
/// cần CSRF token. Luôn <c>HttpOnly</c> + <c>Secure</c> (không gửi cookie qua HTTP trần).
/// </summary>
public static class CookieSecurityDefaults
{
    public static CookieOptions CreateCookieOptions(CookieSameSiteMode mode) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = mode == CookieSameSiteMode.CrossSite ? SameSiteMode.None : SameSiteMode.Lax,
    };

    /// <summary>Cross-site (SameSite=None) → PHẢI kèm CSRF token; same-site → không cần (SameSite đủ chặn CSRF cơ bản).</summary>
    public static bool RequiresCsrf(CookieSameSiteMode mode) => mode == CookieSameSiteMode.CrossSite;
}
