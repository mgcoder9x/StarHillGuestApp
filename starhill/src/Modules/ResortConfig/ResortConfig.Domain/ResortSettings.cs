using Bedrock.Domain.Entities;

namespace ResortConfig.Domain;

/// <summary>
/// Cấu hình vận hành runtime của resort (1-1 với <see cref="Resort"/> qua unique ResortId).
/// Audit (từ <see cref="AuditableEntity"/>) + concurrency token (<see cref="IHasConcurrencyToken"/> →
/// xmin trên Npgsql) vì admin sửa nên cần chống ghi đè đồng thời (CP15).
/// LƯU Ý port: ở Bedrock <c>AuditableEntity</c> KHÔNG mang concurrency (khác resort-qr) → phải implement
/// <see cref="IHasConcurrencyToken"/> tường minh (QR-DV: xem journal).
/// </summary>
public sealed class ResortSettings : AuditableEntity, IHasConcurrencyToken
{
    /// <summary>FK 1-1 tới Resort (unique index enforce).</summary>
    public required Guid ResortId { get; set; }

    // Feature flags — bật/tắt module (Req 14).
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

    /// <summary>Optimistic concurrency token (map → xmin trên Npgsql qua PlatformDbContext).</summary>
    public uint RowVersion { get; set; }
}
