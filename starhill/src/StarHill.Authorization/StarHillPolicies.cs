namespace StarHill.Authorization;

/// <summary>
/// Hằng authorization của sản phẩm StarHill (QR-AD-005/QR-AD-020). Nguồn DUY NHẤT cho tên policy + tên role để
/// mọi module Api admin tham chiếu (không rải chuỗi literal → chống drift). Ngữ nghĩa <b>superset Admin ⊇ Staff</b>
/// hiện thực trong <see cref="StarHillAuthorizationExtensions.AddStarHillAuthorization"/>: policy <see cref="RequireStaff"/>
/// chấp nhận CẢ role <see cref="RoleStaff"/> lẫn <see cref="RoleAdmin"/> → Admin qua được endpoint Staff.
/// </summary>
public static class StarHillPolicies
{
    /// <summary>Tên role trong claim JWT-native <c>role</c> (base <c>RoleClaimType="role"</c>, <c>MapInboundClaims=false</c>).</summary>
    public const string RoleAdmin = "admin";

    /// <summary>Tên role Lễ tân.</summary>
    public const string RoleStaff = "staff";

    /// <summary>Chỉ Admin (Req 7.6: tạo/sửa/xoá phòng + sinh/thu hồi QR).</summary>
    public const string RequireAdmin = "starhill.require-admin";

    /// <summary>Staff hoặc Admin (Admin là superset — Req 11.3). Dùng cho endpoint "xem" vận hành.</summary>
    public const string RequireStaff = "starhill.require-staff";
}
