using Foundation.Application.Abstractions;
using Foundation.Application.Abstractions.Persistence;
using Foundation.Application.Abstractions.Security;
using Foundation.Application.Common;
using Foundation.SharedKernel.DependencyInjection;
using Foundation.SharedKernel.Results;

namespace Foundation.Application.Identity;

/// <summary>
/// Đăng nhập: verify Argon2id (constant-time) → phát access JWT + refresh token (mới family).
/// Chống user-enumeration: LUÔN chạy verify (dùng hash giả nếu user không tồn tại) + lỗi mơ hồ đồng nhất.
/// Rehash-on-login khi tham số hash cũ hơn cấu hình hiện tại.
/// </summary>
public sealed class LoginUseCase : IUseCase<LoginCommand, AuthTokens>, IScopedService
{
    // Hash giả để cân bằng thời gian khi user không tồn tại (chống timing oracle). Cache tĩnh: chỉ tính 1 lần.
    private static string? _dummyHash;

    private readonly IUserAuthStore _users;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RefreshTokenOptions _options;

    public LoginUseCase(
        IUserAuthStore users,
        IRefreshTokenStore refreshTokens,
        IPasswordHasher passwordHasher,
        IRefreshTokenHasher refreshTokenHasher,
        IJwtTokenService jwt,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        RefreshTokenOptions options)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _refreshTokenHasher = refreshTokenHasher;
        _jwt = jwt;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<Result<AuthTokens>> ExecuteAsync(LoginCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var user = await _users.FindByEmailAsync(input.Email, cancellationToken);

        // LUÔN verify (hash thật hoặc hash giả) để thời gian phản hồi không lộ user tồn tại hay không.
        var hashToVerify = user?.PasswordHash ?? GetDummyHash();
        var verification = _passwordHasher.Verify(hashToVerify, input.Password);

        if (user is null || !user.IsActive || verification == PasswordVerificationResult.Failed)
        {
            return Result.Fail<AuthTokens>(AuthErrors.InvalidCredentials);
        }

        // Rehash-on-login: nâng tham số hash không cần reset toàn bộ.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = _passwordHasher.Hash(input.Password);
            await _users.UpdatePasswordHashAsync(user.UserId, newHash, cancellationToken);
        }

        var now = _clock.UtcNow;
        var access = _jwt.Issue(new TokenIssueRequest(user.UserId, user.Roles));

        var refreshSecret = _tokenGenerator.NewToken();
        var record = new RefreshTokenRecord
        {
            Id = Guid.CreateVersion7(),
            UserId = user.UserId,
            FamilyId = Guid.CreateVersion7(),
            TokenHash = _refreshTokenHasher.Hash(refreshSecret),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
        };
        await _refreshTokens.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new AuthTokens(access, refreshSecret, record.ExpiresAt));
    }

    private string GetDummyHash() => _dummyHash ??= _passwordHasher.Hash(Guid.NewGuid().ToString("N"));
}
