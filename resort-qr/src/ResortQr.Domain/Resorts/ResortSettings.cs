using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Resorts;

/// <summary>
/// Cấu hình vận hành runtime của resort (1-1 với <see cref="Resort"/> qua unique <see cref="ResortId"/> — D5).
/// Audit + concurrency (xmin) từ <see cref="AuditableEntity"/> — admin sửa nên cần chống ghi đè.
/// </summary>
public sealed class ResortSettings : AuditableEntity
{
    /// <summary>FK 1-1 tới Resort (unique index enforce).</summary>
    public required Guid ResortId { get; set; }

    // Feature flags — bật/tắt module (14.4).
    public bool FaqEnabled { get; set; } = true;
    public bool ChatEnabled { get; set; } = true;
    public bool HousekeepingEnabled { get; set; } = true;

    // Yêu cầu acknowledge nội quy trước khi dùng module tương ứng (rule gate).
    public bool RequireRuleAckForFaq { get; set; }
    public bool RequireRuleAckForChat { get; set; }
    public bool RequireRuleAckForHousekeeping { get; set; }

    /// <summary>Cửa sổ thao tác (phút) — quá hạn phải quét lại (mặc định 30).</summary>
    public int PortalWindowMinutes { get; set; } = 30;

    /// <summary>Idle expiry của GuestVisit (giờ) — mặc định 24.</summary>
    public int VisitIdleExpiryHours { get; set; } = 24;

    /// <summary>Base URL HTTPS của guest web dùng để sinh nội dung QR (không nhúng số phòng).</summary>
    public string? GuestWebBaseUrl { get; set; }

    public int MaxMessageLength { get; set; } = 2000;
    public int MessageRateLimitPerMinute { get; set; } = 10;
    public int HousekeepingRateLimitPerHour { get; set; } = 12;
}
