using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence.Security;

/// <summary>
/// Impl <see cref="IRefreshTokenStore"/> trên EF (F5/F10/F19, persistence §4). Điểm cốt lõi chống race:
/// <see cref="TryConsumeAsync"/>/<see cref="RevokeFamilyAsync"/> dùng <c>ExecuteUpdateAsync</c> → sinh MỘT câu
/// <c>UPDATE ... WHERE ... AND revoked_at IS NULL</c> chạy NGUYÊN TỬ ở DB (row-level lock), KHÔNG read-rồi-write,
/// KHÔNG lock ứng dụng → 2 request đồng thời chỉ 1 câu ăn 1 row = đúng 1 thắng (fix GỐC race rotation).
/// <para>
/// <see cref="GetByHashAsync"/> trả theo hash BẤT KỂ revoked/expiry (AD-031) để use case làm reuse-detection.
/// <see cref="AddAsync"/> chỉ STAGE (commit ở <c>IUnitOfWork.SaveChangesAsync</c>) → consume + insert token mới
/// nằm cùng một <c>ExecuteInTransactionAsync</c> = all-or-nothing (§7.4). Race đa-connection thật → task 8.3.
/// </para>
/// </summary>
public sealed class EfRefreshTokenStore(PlatformDbContext context, IClock clock) : IRefreshTokenStore
{
    private const string FamilyRevokedReason = "family_revoked";

    public async Task<RefreshTokenSnapshot?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return await context.Set<RefreshTokenRecord>()
            .AsNoTracking()
            .Where(r => r.TokenHash == tokenHash)
            .Select(r => new RefreshTokenSnapshot(r.Id, r.UserId, r.FamilyId, r.TokenHash, r.ExpiresAt, r.RevokedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> TryConsumeAsync(
        Guid tokenId,
        DateTimeOffset now,
        string reason,
        Guid replacedByTokenId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        // consume-if-not-revoked NGUYÊN TỬ: UPDATE ... WHERE id=@id AND revoked_at IS NULL.
        var affected = await context.Set<RefreshTokenRecord>()
            .Where(r => r.Id == tokenId && r.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.RevokedAt, now)
                    .SetProperty(r => r.RevokedReason, reason)
                    .SetProperty(r => r.ReplacedByTokenId, (Guid?)replacedByTokenId),
                ct)
            .ConfigureAwait(false);

        // 1 = chính lời gọi này thu hồi (thắng); 0 = request khác đã thu hồi (race/reuse).
        return affected == 1;
    }

    public Task AddAsync(RefreshTokenSnapshot newToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(newToken);

        // Chỉ STAGE — ghi thật ở SaveChanges (cùng transaction với consume). CreatedAt do store đóng dấu (IClock).
        context.Set<RefreshTokenRecord>().Add(new RefreshTokenRecord
        {
            Id = newToken.Id,
            UserId = newToken.UserId,
            FamilyId = newToken.FamilyId,
            TokenHash = newToken.TokenHash,
            ExpiresAt = newToken.ExpiresAt,
            CreatedAt = clock.UtcNow,
            RevokedAt = newToken.RevokedAt,
        });
        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default) =>
        // Thu hồi mọi token CÒN hiệu lực trong family (reuse-detection / logout-all) — nguyên tử một câu UPDATE.
        context.Set<RefreshTokenRecord>()
            .Where(r => r.FamilyId == familyId && r.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.RevokedAt, clock.UtcNow)
                    .SetProperty(r => r.RevokedReason, FamilyRevokedReason),
                ct);
}
