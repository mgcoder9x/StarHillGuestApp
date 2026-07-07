using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Identity;

/// <summary>
/// Kho người dùng cho xác thực. App tự hiện thực trên entity/EF riêng; base chỉ định nghĩa hợp đồng.
/// </summary>
public interface IUserAuthStore : IScopedService
{
    Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Cập nhật hash mật khẩu (rehash-on-login). Chỉ stage thay đổi; commit qua UnitOfWork.</summary>
    Task UpdatePasswordHashAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kho refresh token. Add/Update chỉ stage vào ChangeTracker; ghi thật qua <c>IUnitOfWork.SaveChangesAsync</c>.
/// </summary>
public interface IRefreshTokenStore : IScopedService
{
    Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tiêu thụ NGUYÊN TỬ token (one-time-use, chống race): đánh dấu revoked CHỈ KHI đang còn hiệu lực
    /// (RevokedAt IS NULL). Trả <c>true</c> nếu CHÍNH lời gọi này thực hiện thu hồi; <c>false</c> nếu token
    /// đã bị thu hồi bởi request khác (race/reuse). EF impl PHẢI atomic: conditional UPDATE ... WHERE
    /// revoked_at IS NULL + kiểm rows-affected (hoặc optimistic concurrency token) — KHÔNG read-rồi-write.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid tokenId, DateTimeOffset revokedAt, string reason, Guid replacedByTokenId, CancellationToken cancellationToken = default);

    /// <summary>Thu hồi mọi token còn hiệu lực trong một family (reuse-detection / logout-all).</summary>
    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAt, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Băm refresh token để tra cứu/lưu. Token là chuỗi ENTROPY CAO (CSPRNG) nên hash nhanh (SHA-256) là đủ
/// — KHÁC mật khẩu (entropy thấp) cần Argon2. Phải TẤT ĐỊNH để lookup theo hash.
/// </summary>
public interface IRefreshTokenHasher : ISingletonService
{
    string Hash(string refreshToken);
}
