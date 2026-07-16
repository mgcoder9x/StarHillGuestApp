using Bedrock.Application.Ports.Security;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Seed tài khoản admin khởi tạo — IDEMPOTENT (F.1c/QR-AD-039). Chỉ tạo khi username CHƯA tồn tại (chạy lại không
/// nhân đôi). Password THÔ băm qua <see cref="IPasswordHasher"/> (Argon2) — KHÔNG lưu thô. Credential do CALLER
/// (Host) đọc từ config <c>Identity:SeedAdmin:*</c> và truyền vào; Host CHỈ gọi khi cả hai có (dev/compose) →
/// prod KHÔNG cấu hình = KHÔNG seed (admin tạo out-of-band, tránh password dev lọt prod — F35).
/// </summary>
public sealed class IdentityUserSeeder
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public IdentityUserSeeder(IdentityDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>Tạo admin (role Admin, IsActive) nếu username chưa tồn tại. Trả true nếu vừa tạo, false nếu đã có.</summary>
    public async Task<bool> SeedAdminAsync(string username, string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalized = username.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Username == normalized, ct).ConfigureAwait(false);
        if (exists)
        {
            return false;
        }

        _db.Users.Add(new IdentityUser
        {
            Username = normalized,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true,
            DisplayName = "Administrator",
        });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
