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

/// <summary>
/// Loại dịch vụ dọn phòng khách chọn khi tạo yêu cầu (Req 6.1) — khớp form guest (serviceFull/Towel/Trash/Refill).
/// <c>null</c> với ticket do nhân viên tạo chủ động (Req 6.9 — vận hành, không qua form). Lưu STRING (mirror
/// <see cref="HousekeepingStatus"/>): đọc được ở DB, ổn định, provider-agnostic.
/// </summary>
public enum HousekeepingServiceType
{
    /// <summary>Dọn phòng đầy đủ.</summary>
    Full,

    /// <summary>Chỉ thay khăn.</summary>
    Towel,

    /// <summary>Chỉ đổ rác.</summary>
    Trash,

    /// <summary>Bổ sung tiện ích/vật dụng.</summary>
    Refill,
}

/// <summary>
/// Thời điểm khách muốn được phục vụ (Req 6.1) — khớp form guest (timeNow/time1h/timeSpecific). Khi
/// <see cref="SpecificTime"/> thì <see cref="HousekeepingTicket.PreferredTimeText"/> mang giờ <c>HH:mm</c> (24h). Lưu STRING.
/// </summary>
public enum HousekeepingPreferredTime
{
    /// <summary>Càng sớm càng tốt (ngay).</summary>
    AsSoonAsPossible,

    /// <summary>Trong vòng một giờ.</summary>
    WithinOneHour,

    /// <summary>Giờ cụ thể — kèm <see cref="HousekeepingTicket.PreferredTimeText"/> = <c>HH:mm</c>.</summary>
    SpecificTime,
}
