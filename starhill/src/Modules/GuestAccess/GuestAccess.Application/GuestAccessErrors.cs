using Bedrock.Domain.Results;

namespace GuestAccess.Application;

/// <summary>
/// Mã lỗi guest-facing của module GuestAccess (∈ catalog §14, gác bởi ErrorCodeSnapshotTests — QR-AD-018).
/// NON-DISCLOSURE (CP1): token không tồn tại / bị thu hồi / phòng đã xóa mềm gộp CHUNG <see cref="QrInvalid"/>
/// (IRoomTokenResolver cố ý trả null cho cả nhóm — không lộ phòng khác). Phòng resolve được nhưng không Active →
/// <see cref="RoomInactive"/>. Dữ liệu nền thiếu/không hợp lệ → <see cref="ConfigurationUnavailable"/> (fail-closed).
/// </summary>
public static class GuestAccessErrors
{
    /// <summary>Token QR không hợp lệ (không tồn tại / thu hồi / phòng đã xóa) — KHÔNG lộ thông tin phòng (Req 1.4).</summary>
    public static Error QrInvalid =>
        Error.NotFound("qr_invalid", "Mã QR không hợp lệ.");

    /// <summary>Phòng tồn tại nhưng không ở trạng thái phục vụ (Inactive/Maintenance) — Req 1.4.</summary>
    public static Error RoomInactive =>
        Error.Conflict("room_inactive", "Phòng hiện không khả dụng.");

    /// <summary>Cấu hình resort nền thiếu/không hợp lệ (chưa seed settings/ngôn ngữ mặc định) — fail-closed (QR-AD-024).</summary>
    public static Error ConfigurationUnavailable =>
        Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng.");
}
