namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Chế độ triển khai cookie/CORS (F17). Quyết định SameSite policy + có cần CSRF token + độ siết CORS.
/// </summary>
public enum CookieSameSiteMode
{
    /// <summary>FE và API cùng site (same-origin/subdomain) → SameSite=Lax, KHÔNG cần CSRF token, CORS đóng.</summary>
    SameSite = 0,

    /// <summary>FE khác site với API → SameSite=None (bắt buộc Secure) + CSRF token + CORS mở đúng origin khai.</summary>
    CrossSite = 1,
}
