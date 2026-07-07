namespace ResortQr.Api.GuestAccess;

/// <summary>
/// Cấu hình cookie phiên khách (bind section "Guest"; mặc định an toàn nếu thiếu config). 15 §.
/// </summary>
public sealed class GuestOptions
{
    public const string SectionName = "Guest";

    /// <summary>Tên cookie thiết bị khách (không lộ ngữ nghĩa nhạy cảm).</summary>
    public string CookieName { get; set; } = "shq_guest";

    /// <summary>Hạn cookie (ngày) — thiết bị dài hạn (30–90 ngày).</summary>
    public int SessionCookieDays { get; set; } = 60;
}
