namespace ResortConfig.Contracts.Queries;

/// <summary>
/// Snapshot READ-ONLY của ResortSettings cho module KHÁC tiêu thụ qua Contracts (F30) — DTO thuần, KHÔNG lộ
/// entity Domain. Mang trọn cấu hình vận hành (đọc cohesive của aggregate settings): consumer đầu tiên là
/// <c>Rooms.RenderQrPng</c> (dùng <see cref="GuestWebBaseUrl"/>); GuestAccess/Rules/Faq/Housekeeping (sau) dùng
/// portal-window/idle-expiry + feature flags + rate limits — nên trả cả object là tự nhiên, không phải gold-plate.
/// </summary>
public sealed record ResortSettingsSnapshot(
    Guid ResortId,
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RequireRuleAckForFaq,
    bool RequireRuleAckForChat,
    bool RequireRuleAckForHousekeeping,
    int PortalWindowMinutes,
    int VisitIdleExpiryHours,
    string? GuestWebBaseUrl,
    int MaxMessageLength,
    int MessageRateLimitPerMinute,
    int HousekeepingRateLimitPerHour);

/// <summary>
/// Query đọc ResortSettings (single-resort → một bản ghi). Trả <c>null</c> nếu chưa seed. Impl EF ở
/// Infrastructure (đăng ký thủ công scoped — Contracts không mang marker DI). Module khác chạm qua interface này.
/// </summary>
public interface IResortSettingsQuery
{
    Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default);
}
