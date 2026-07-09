namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Cấu hình HTTP hardening (F16/F17) — bind từ section <see cref="SectionName"/>. Collection get-only để binder
/// populate (tránh CA1819/CA2227). Trung lập nghiệp vụ: KHÔNG hardcode origin/proxy — Host khai qua config.
/// </summary>
public sealed class HttpSecurityOptions
{
    public const string SectionName = "HttpSecurity";

    /// <summary>IP proxy TIN CẬY (reverse proxy/load balancer) để chấp nhận X-Forwarded-* (F16 — không tin mù).</summary>
    public IList<string> KnownProxies { get; } = new List<string>();

    /// <summary>Network tin cậy dạng CIDR "ip/prefix" (vd "10.0.0.0/8").</summary>
    public IList<string> KnownNetworks { get; } = new List<string>();

    /// <summary>Số hop X-Forwarded-For được xử lý (mặc định 1 = một proxy trước API).</summary>
    public int ForwardLimit { get; set; } = 1;

    /// <summary>Số request cho mỗi cửa sổ / mỗi IP (rate-limit biên).</summary>
    public int RateLimitPermitLimit { get; set; } = 100;

    /// <summary>Độ dài cửa sổ rate-limit (giây).</summary>
    public int RateLimitWindowSeconds { get; set; } = 60;

    /// <summary>Số request được xếp hàng chờ khi vượt (0 = từ chối ngay).</summary>
    public int RateLimitQueueLimit { get; set; }

    /// <summary>Chế độ cookie/CORS (F17).</summary>
    public CookieSameSiteMode CookieSameSiteMode { get; set; } = CookieSameSiteMode.SameSite;

    /// <summary>Origin được phép khi <see cref="CookieSameSiteMode.CrossSite"/> (bỏ qua ở chế độ same-site).</summary>
    public IList<string> CorsAllowedOrigins { get; } = new List<string>();

    /// <summary>Tên CORS policy áp ở pipeline slot #8.</summary>
    public string CorsPolicyName { get; set; } = "BedrockDefault";
}
