using Bedrock.Domain.Entities;

namespace Identity.Domain;

/// <summary>
/// Người dùng nội bộ (Admin/Staff) đăng nhập quản trị (Req 11 — QR-AD-038). <see cref="Username"/> chuẩn hóa
/// lower-invariant khi lưu + lookup (unique <c>ux_identity_user_username</c>). <see cref="PasswordHash"/> là chuỗi
/// Argon2id PHC (base <c>Argon2idPasswordHasher</c>) — KHÔNG bao giờ lưu/log mật khẩu thô (F15/Req 11.6).
/// <see cref="IsActive"/>=false → không đăng nhập được. v1 KHÔNG mang ResortId (single-resort — QR-AD-038) và
/// KHÔNG concurrency token (chưa có màn CRUD user đua-ghi). <c>CreatedAt</c> do interceptor audit tự set.
/// </summary>
public sealed class IdentityUser : AuditableEntity
{
    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public string? DisplayName { get; set; }
}
