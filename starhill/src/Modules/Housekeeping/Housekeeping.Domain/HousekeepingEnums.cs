namespace Housekeeping.Domain;

/// <summary>
/// Vòng đời ticket dọn phòng (Req 6). <see cref="Requested"/>/<see cref="InProgress"/> = MỞ (chịu ràng buộc
/// "1 ticket mở/phòng"); <see cref="Done"/>/<see cref="Cancelled"/> = terminal. Lưu STRING (HasConversion) để
/// partial-unique filter đọc được + ổn định (mirror Rooms RoomStatus — QR-AD).
/// </summary>
public enum HousekeepingStatus
{
    Requested,
    InProgress,
    Done,
    Cancelled,
}

/// <summary>Cách nhân viên hoàn tất ticket (Req 6.4/6.5) — chỉ set khi chuyển <see cref="HousekeepingStatus.Done"/>.</summary>
public enum HousekeepingCompletionMethod
{
    /// <summary>Nhân viên chọn phòng trong app → xác nhận đã dọn (cách CHÍNH).</summary>
    App,

    /// <summary>Nhân viên (đã đăng nhập) quét QR phòng → hoàn tất (lối tắt; QR chỉ định phòng, không thay đăng nhập).</summary>
    StaffScan,
}

/// <summary>Chủ thể gây chuyển trạng thái (nhật ký đối soát — Req 6.8).</summary>
public enum HousekeepingActorType
{
    Guest,
    Staff,
    System,
}
