namespace ResortQr.Application.GuestAccess;

/// <summary>
/// Input resolve: token thô từ URL + khóa phiên hiện tại (raw cookie, có thể null với thiết bị mới).
/// Truyền session qua tham số (KHÔNG HttpContext) → use case thuần, test được.
/// </summary>
public sealed record ResolveTokenInput(string RawToken, string? CurrentSessionKey);

/// <summary>
/// Kết quả resolve. <see cref="IssuedSessionKey"/> != null → thiết bị mới → Api set cookie (raw secret,
/// KHÔNG đưa vào JSON body). Các trường ack/ngôn ngữ đầy đủ hoãn tới wave Rules/i18n.
/// </summary>
public sealed record ResolveTokenResult(
    Guid RoomId,
    string RoomNumber,
    Guid ResortId,
    string ResortName,
    Guid VisitId,
    DateTimeOffset PortalWindowExpiresAt,
    string? IssuedSessionKey,
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    string? DefaultLanguageCode);
