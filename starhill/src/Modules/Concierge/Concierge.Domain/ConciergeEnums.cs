namespace Concierge.Domain;

/// <summary>
/// Trạng thái hội thoại khách↔lễ tân (Req 5). <see cref="Open"/> = đang mở (khách/nhân viên nhắn tiếp bình thường);
/// <see cref="Closed"/> = đã đóng (nhân viên đóng, hoặc System đóng khi visit kết thúc — CP9). Khi khách gửi tin lại
/// mà visit CÒN hiệu lực → hội thoại được MỞ LẠI (Closed→Open) chứ KHÔNG tạo hội thoại thứ hai (Req 5.2/5.10). Lưu
/// STRING (HasConversion) để đọc được + ổn định (mirror HousekeepingStatus/RoomStatus — QR-AD enum-string).
/// </summary>
public enum ConversationStatus
{
    Open,
    Closed,
}

/// <summary>
/// Chủ thể gửi tin nhắn (Req 5). <see cref="Guest"/> = khách (guest session); <see cref="Staff"/> = nhân viên lễ tân
/// (đã đăng nhập); <see cref="System"/> = tin hệ thống (ví dụ thông báo đóng hội thoại khi visit kết thúc). Lưu STRING.
/// </summary>
public enum MessageSenderType
{
    Guest,
    Staff,
    System,
}
