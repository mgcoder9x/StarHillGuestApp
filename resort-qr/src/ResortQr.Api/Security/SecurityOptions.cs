namespace ResortQr.Api.Security;

/// <summary>
/// Cấu hình bảo mật HTTP (Req 20): CSP + CORS allowlist + HSTS. Origin CORS khai báo tường minh
/// (KHÔNG wildcard). HTTPS redirect KHÔNG do app làm — reverse proxy đảm nhận (DEC-025) để tránh
/// redirect loop khi proxy đã terminate TLS và forward http tới Kestrel loopback.
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>CSP mặc định: chỉ 'self' + cho phép WebSocket (wss:) cho SignalR; cấm nhúng iframe.</summary>
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; frame-ancestors 'none'; connect-src 'self' wss:";

    /// <summary>Origin được phép CORS (guest, admin). Rỗng = không cho cross-origin nào (same-origin deploy).</summary>
    public string[] AllowedCorsOrigins { get; set; } = [];

    /// <summary>HSTS max-age (giây). Mặc định 365 ngày (Req 20.5). Header chỉ phát trên HTTPS.</summary>
    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;
}
