namespace GuestAccess.Application;

/// <summary>
/// Input resolve. <see cref="RawToken"/> = capability token QR (từ đường dẫn Guest Web, gửi qua body — QR-DV-006).
/// <see cref="CurrentSessionKey"/> = raw cookie thiết bị hiện có (nếu client đã có), dùng để nối lại session; null
/// nếu chưa có → cấp session mới. KHÔNG bao giờ log hai field này (Req 11.6).
/// </summary>
public sealed record ResolveTokenInput(string RawToken, string? CurrentSessionKey);

/// <summary>
/// Kết quả resolve (đủ để Api dựng response). <see cref="IssuedSessionKey"/> = raw key CHỈ set khi phát session
/// mới → Api set cookie; null nếu nối lại session cũ. KHÔNG chứa hash/ExpiresAt idle/nội bộ (QR-AD-025).
/// </summary>
public sealed record ResolveTokenResult(
    Guid RoomId,
    string RoomNumber,
    string? Building,
    int? Floor,
    Guid ResortId,
    string ResortName,
    string? LogoUrl,
    Guid VisitId,
    DateTimeOffset PortalWindowExpiresAt,
    IReadOnlyList<string> Languages,
    string DefaultLanguageCode,
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RequireRuleAckForFaq,
    bool RequireRuleAckForChat,
    bool RequireRuleAckForHousekeeping,
    string? IssuedSessionKey);
