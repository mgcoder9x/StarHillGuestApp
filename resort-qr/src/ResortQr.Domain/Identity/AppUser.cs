using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Identity;

/// <summary>
/// Tài khoản quản trị (Admin/Staff). Mật khẩu lưu dạng PHC string (Argon2id).
/// Audit + concurrency (xmin) từ <see cref="AuditableEntity"/>.
/// </summary>
public sealed class AppUser : AuditableEntity
{
    public required Guid ResortId { get; set; }

    /// <summary>Email đăng nhập (unique, chuẩn hoá lowercase ở use case).</summary>
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>PHC string Argon2id (băm ở Infrastructure).</summary>
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Staff;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }
}
