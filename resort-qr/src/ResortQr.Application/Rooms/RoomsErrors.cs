using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Rooms;

/// <summary>
/// Mã lỗi module Rooms/QR (∈ catalog 14). Nghiệp vụ riêng của app → khai ở tầng Application (không ở base).
/// </summary>
public static class RoomsErrors
{
    /// <summary>Trùng số phòng đang sống (ux_room_number) — Req 16.6.</summary>
    public static Error RoomNumberTaken =>
        Error.Validation("validation_error", "Số phòng đã tồn tại.");

    /// <summary>Không tạo/rotate được token (hết retry unique, race ux_qr_active, phòng không Active — Req 16.7).</summary>
    public static Error QrGenerationFailed =>
        Error.Conflict("qr_generation_failed", "Không thể sinh mã QR, vui lòng thử lại.");

    /// <summary>Phòng không tồn tại (hoặc đã xóa mềm).</summary>
    public static Error RoomNotFound =>
        Error.NotFound("not_found", "Không tìm thấy phòng.");

    /// <summary>Cấu hình thiếu/không hợp lệ (vd GuestWebBaseUrl thiếu hoặc không https) — Req 15.6.</summary>
    public static Error InvalidConfiguration =>
        Error.Validation("invalid_configuration", "Cấu hình chưa hợp lệ (thiếu GuestWebBaseUrl https).");
}
