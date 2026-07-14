namespace ResortConfig.Contracts.Queries;

/// <summary>
/// Snapshot cấu hình GUEST-FACING cohesive cho một resort — phục vụ GuestAccess.resolve (QR-AD-024). Gộp phần
/// trình bày (tên/logo), ngôn ngữ (danh sách bật + đúng một mặc định) và cờ tính năng/portal-window/idle mà
/// endpoint resolve cần trả cho guest-web. Tách khỏi <see cref="IResortSettingsQuery"/> (đang phục vụ Rooms.qr —
/// chỉ settings vận hành) để không phình DTO đa-trách-nhiệm; query theo <c>resortId</c> (không single-resort ngầm)
/// mở đường multi-resort + cho phép consumer kiểm <c>ResortId</c> khớp phòng resolve được.
/// DTO thuần (Contracts không mang marker DI); impl EF đăng ký thủ công scoped ở Infrastructure.
/// </summary>
public sealed record ResortGuestConfig(
    Guid ResortId,
    string ResortName,
    string? LogoUrl,
    IReadOnlyList<string> EnabledLanguageCodes,
    string DefaultLanguageCode,
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RequireRuleAckForFaq,
    bool RequireRuleAckForChat,
    bool RequireRuleAckForHousekeeping,
    int PortalWindowMinutes,
    int VisitIdleExpiryHours);

/// <summary>
/// Đọc cấu hình guest-facing của một resort. Trả <c>null</c> (FAIL-CLOSED — QR-AD-024) khi bất kỳ mảnh nền BẮT
/// BUỘC nào thiếu/không hợp lệ: resort không tồn tại, chưa có <c>ResortSettings</c>, hoặc KHÔNG có ngôn ngữ mặc
/// định hợp lệ (enabled + IsDefault). Consumer (GuestAccess.resolve) map <c>null</c> → <c>configuration_unavailable</c>
/// và KHÔNG tạo/touch session/visit — tuyệt đối không "bật ngầm" tính năng bằng default khi dữ liệu nền mất.
/// </summary>
public interface IResortGuestConfigQuery
{
    Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default);
}
