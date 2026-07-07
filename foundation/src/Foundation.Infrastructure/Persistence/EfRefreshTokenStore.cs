using Foundation.Application.Identity;
using Microsoft.EntityFrameworkCore;

namespace Foundation.Infrastructure.Persistence;

/// <summary>
/// Store refresh token trên EF. <see cref="TryConsumeAsync"/>/<see cref="RevokeFamilyAsync"/> dùng
/// <c>ExecuteUpdateAsync</c> → sinh MỘT câu <c>UPDATE ... WHERE ... AND revoked_at IS NULL</c> chạy
/// NGUYÊN TỬ ở DB (row-level lock). Đây là fix GỐC race rotation (không lock ứng dụng, không read-rồi-write):
/// hai request đồng thời chỉ 1 câu update ăn 1 row → đúng 1 request thắng.
/// </summary>
public sealed class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly FoundationDbContext _context;

    public EfRefreshTokenStore(FoundationDbContext context) => _context = context;

    /// <summary>Tra token theo hash (tracked — để logout mutate rồi <see cref="UpdateAsync"/> lưu được).</summary>
    public Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

    public Task AddAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default)
    {
        // Chỉ STAGE vào ChangeTracker; ghi thật ở IUnitOfWork.SaveChangesAsync. Id client-side → Add đồng bộ.
        _context.RefreshTokens.Add(record);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RefreshTokenRecord record, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Update(record);
        return Task.CompletedTask;
    }

    public async Task<bool> TryConsumeAsync(
        Guid tokenId,
        DateTimeOffset revokedAt,
        string reason,
        Guid replacedByTokenId,
        CancellationToken cancellationToken = default)
    {
        // consume-if-not-revoked NGUYÊN TỬ: UPDATE ... WHERE id=@id AND revoked_at IS NULL.
        var affected = await _context.RefreshTokens
            .Where(r => r.Id == tokenId && r.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.RevokedAt, revokedAt)
                .SetProperty(r => r.RevokedReason, reason)
                .SetProperty(r => r.ReplacedByTokenId, (Guid?)replacedByTokenId), cancellationToken)
            .ConfigureAwait(false);

        // 1 = chính lời gọi này thu hồi; 0 = request khác đã thu hồi (race/reuse).
        return affected == 1;
    }

    public Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken = default) =>
        // Thu hồi mọi token CÒN hiệu lực trong family (reuse-detection / logout-all) — nguyên tử.
        _context.RefreshTokens
            .Where(r => r.FamilyId == familyId && r.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.RevokedAt, revokedAt)
                .SetProperty(r => r.RevokedReason, reason), cancellationToken);
}
