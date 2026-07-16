namespace Identity.Domain;

/// <summary>
/// Vai trò người dùng nội bộ (Req 11.1). CHỈ hai vai (sản phẩm 60 phòng — QR-AD-005): <see cref="Admin"/> là
/// SUPERSET của <see cref="Staff"/> (Admin qua được endpoint Staff — ngữ nghĩa ở StarHillPolicies). Lưu STRING
/// trong DB (ổn định, đọc được) — mirror GuestVisitStatus. Map sang claim JWT-native <c>role</c> = "admin"/"staff".
/// </summary>
public enum UserRole
{
    Admin,
    Staff,
}
