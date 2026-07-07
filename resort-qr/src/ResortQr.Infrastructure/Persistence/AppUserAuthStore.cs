using ResortQr.Application.Identity;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Kho người dùng cho xác thực trên entity <c>AppUser</c>. Nằm trong namespace Persistence → BỊ LOẠI khỏi
/// Scrutor auto-scan (như UnitOfWork/RefreshTokenStore) vì hard-require <see cref="AppDbContext"/>;
/// đăng ký TƯỜNG MINH qua <c>AddResortQrDatabase</c> (cùng nơi wire DbContext) để fail-fast rõ ràng.
/// Chỉ stage thay đổi; commit qua <c>IUnitOfWork</c>.
/// </summary>
public sealed class AppUserAuthStore : IUserAuthStore
{
    private readonly AppDbContext _db;

    public AppUserAuthStore(AppDbContext db) => _db = db;

    public async Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => new AuthenticatedUser(u.Id, u.Email, u.PasswordHash, new[] { u.Role.ToString() }, u.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuthenticatedUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new AuthenticatedUser(u.Id, u.Email, u.PasswordHash, new[] { u.Role.ToString() }, u.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is not null)
        {
            user.PasswordHash = passwordHash;
        }
    }
}
