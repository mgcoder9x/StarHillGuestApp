namespace ResortQr.Domain.Identity;

/// <summary>
/// Vai trò người dùng quản trị. Lưu dạng string (DEC-006) — giá trị ổn định, không đổi tên.
/// </summary>
public enum UserRole
{
    /// <summary>Toàn quyền: rooms/QR/users/settings + vận hành.</summary>
    Admin,

    /// <summary>Vận hành: nội dung/tin nhắn/ticket; chỉ xem phòng.</summary>
    Staff,
}
