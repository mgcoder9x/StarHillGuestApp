using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.GuestAccess;

/// <summary>
/// Mã lỗi guest-access (∈ catalog 14 §2.1). KHÔNG lộ phòng khác khi token lỗi (Req 1.4).
/// </summary>
public static class GuestAccessErrors
{
    /// <summary>Token không tồn tại.</summary>
    public static Error QrInvalid =>
        Error.NotFound("qr_invalid", "Mã QR không hợp lệ.");

    /// <summary>Token đã bị thu hồi (rotate).</summary>
    public static Error QrRevoked =>
        Error.NotFound("qr_revoked", "Mã QR đã thay đổi, vui lòng hỏi lễ tân.");

    /// <summary>Phòng đã xóa mềm hoặc không ở trạng thái Active.</summary>
    public static Error RoomInactive =>
        Error.Conflict("room_inactive", "Phòng hiện không khả dụng.");

    /// <summary>Quá cửa sổ thao tác / visit không Active — cần quét QR lại.</summary>
    public static Error SessionExpired =>
        Error.Forbidden("session_expired", "Phiên đã hết hạn, vui lòng quét lại mã QR.");
}
