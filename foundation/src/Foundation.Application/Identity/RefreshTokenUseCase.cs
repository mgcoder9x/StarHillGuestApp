using Foundation.Application.Abstractions;
using Foundation.Application.Abstractions.Persistence;
using Foundation.Application.Abstractions.Security;
using Foundation.Application.Common;
using Foundation.SharedKernel.DependencyInjection;
using Foundation.SharedKernel.Results;

namespace Foundation.Application.Identity;

/// <summary>
/// Đổi refresh token lấy access mới, có ROTATION (one-time-use) + REUSE DETECTION theo family:
/// nếu trình lại token đã bị thu hồi/đã xoay → dấu hiệu bị đánh cắp → thu hồi CẢ family (buộc login lại).
/// </summary>
public sealed class RefreshTokenUseCase : IUseCase<RefreshCommand, AuthTokens>, IScopedService
{
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IUserAuthStore _users;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RefreshTokenOptions _options;

    public RefreshTokenUseCase(
        IRefreshTokenStore refreshTokens,
        IUserAuthStore users,
        IRefreshTokenHasher refreshTokenHasher,
        IJwtTokenService jwt,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        RefreshTokenOptions options)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _refreshTokenHasher = refreshTokenHasher;
        _jwt = jwt;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<Result<AuthTokens>> ExecuteAsync(RefreshCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var now = _clock.UtcNow;
        var presentedHash = _refreshTokenHasher.Hash(input.RefreshToken);
        var record = await _refreshTokens.FindByHashAsync(presentedHash, cancellationToken);

        if (record is null)
        {
            return Result.Fail<AuthTokens>(AuthErrors.InvalidRefreshToken);
        }

        // Trình lại token ĐÃ bị thu hồi (đã xoay/đăng xuất) → nghi bị đánh cắp → thu hồi cả family.
        if (record.RevokedAt is not null)
        {
            await _refreshTokens.RevokeFamilyAsync(record.FamilyId, now, "reuse_detected", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail<AuthTokens>(AuthErrors.InvalidRefreshToken);
        }

        if (now >= record.ExpiresAt)
        {
            return Result.Fail<AuthTokens>(AuthErrors.RefreshTokenExpired);
        }

        var user = await _users.FindByIdAsync(record.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            // User biến mất/khoá → vô hiệu cả family để chắc chắn.
            await _refreshTokens.RevokeFamilyAsync(record.FamilyId, now, "user_inactive", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail<AuthTokens>(AuthErrors.InvalidRefreshToken);
        }

        // Rotation: cấp token mới cùng family. ATOMIC consume token cũ (chống race 2 request đồng thời).
        var newSecret = _tokenGenerator.NewToken();
        var newRecord = new RefreshTokenRecord
        {
            Id = Guid.CreateVersion7(),
            UserId = user.UserId,
            FamilyId = record.FamilyId,
            TokenHash = _refreshTokenHasher.Hash(newSecret),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
        };

        // Chỉ MỘT request thắng việc tiêu thụ token cũ (consume-if-not-revoked nguyên tử).
        var consumed = await _refreshTokens.TryConsumeAsync(record.Id, now, "rotated", newRecord.Id, cancellationToken);
        if (!consumed)
        {
            // Request khác đã tiêu thụ token này (race) hoặc token vừa bị revoke → nghi bị đánh cắp → thu hồi family.
            await _refreshTokens.RevokeFamilyAsync(record.FamilyId, now, "reuse_detected", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail<AuthTokens>(AuthErrors.InvalidRefreshToken);
        }

        await _refreshTokens.AddAsync(newRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var access = _jwt.Issue(new TokenIssueRequest(user.UserId, user.Roles));
        return Result.Ok(new AuthTokens(access, newSecret, newRecord.ExpiresAt));
    }
}
