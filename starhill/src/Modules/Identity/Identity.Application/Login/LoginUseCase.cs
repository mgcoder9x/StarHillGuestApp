using System.Security.Claims;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Identity.Application.RefreshToken;
using Identity.Domain;

namespace Identity.Application.Login;

/// <summary>
/// Đăng nhập nội bộ (Admin/Staff) — F.1b/QR-AD-038/039. Server-authoritative: lookup username → verify password
/// (Argon2 base) → phát access-token (<c>sub</c>+<c>role</c>) + refresh-token gia đình MỚI. Value-returning →
/// TỰ mở transaction hẹp (mirror <see cref="RefreshAccessTokenUseCase"/>) vì có ghi refresh-token.
/// <para>
/// <b>Chống user-enumeration:</b> (1) MỌI thất bại (user lạ / sai pass / inactive) → MỘT mã <see cref="AuthErrors.InvalidCredentials"/>
/// (không oracle qua nội dung); (2) user lạ vẫn CHẠY một <c>Verify</c> giả (hash dummy tạo bằng CHÍNH hasher production
/// → cùng cost Argon2) để cân bằng thời gian → không rò tồn-tại-username qua latency. Kết hợp rate-limiter base (slot #9).
/// </para>
/// <para><b>role→claim:</b> phát claim JWT-native <c>role</c> = admin|staff (StarHillPolicies) → policy RequireAdmin/
/// RequireStaff authorize đúng ngay sau login (hợp đồng đã valid: base RoleClaimType="role", MapInboundClaims=false).</para>
/// </summary>
public sealed class LoginUseCase : IUseCase<LoginCommand, LoginResult>
{
    // Dummy hash tạo LƯỜI bằng hasher production (đúng cost Argon2) rồi cache process-wide → user-lạ verify giả
    // có cùng độ trễ như verify thật (chống timing-enumeration). Tính 2 lần (race hiếm) vô hại (giá trị nào cũng dùng được).
    private static string? _dummyHash;

    private readonly IRepository<IdentityUser> _users;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _passwordHasher;

    public LoginUseCase(
        IRepository<IdentityUser> users,
        IRefreshTokenStore refreshTokens,
        IUnitOfWork unitOfWork,
        IClock clock,
        ITokenGenerator tokenGenerator,
        IJwtTokenService jwt,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenGenerator = tokenGenerator;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(LoginCommand input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var username = input.Username.Trim().ToLowerInvariant();
        var user = await _users.FirstOrDefaultAsync(u => u.Username == username, ct).ConfigureAwait(false);

        // Timing-equalization: user lạ vẫn verify (dummy) rồi trả cùng lỗi chung (không rò tồn tại qua latency/nội dung).
        if (user is null)
        {
            _dummyHash ??= _passwordHasher.Hash("timing-equalization-dummy");
            _ = _passwordHasher.Verify(input.Password, _dummyHash);
            return Result.Failure<LoginResult>(AuthErrors.InvalidCredentials);
        }

        var passwordOk = _passwordHasher.Verify(input.Password, user.PasswordHash);
        if (!passwordOk || !user.IsActive)
        {
            return Result.Failure<LoginResult>(AuthErrors.InvalidCredentials);
        }

        var now = _clock.UtcNow;
        var rawRefresh = _tokenGenerator.NewToken();
        var expiresAt = now.Add(RefreshAccessTokenUseCase.RefreshTokenLifetime);

        // Tạo gia đình refresh MỚI (login khởi tạo family; rotation sau do RefreshAccessTokenUseCase). Ghi trong transaction.
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await _refreshTokens.AddAsync(
                    new RefreshTokenSnapshot(
                        Guid.CreateVersion7(),
                        user.Id,
                        FamilyId: Guid.CreateVersion7(),
                        RefreshTokenHashing.Sha256Hex(rawRefresh),
                        expiresAt,
                        RevokedAt: null),
                    token).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(token).ConfigureAwait(false);
                return true;
            },
            ct).ConfigureAwait(false);

        var accessToken = _jwt.Issue(BuildIdentity(user));
        return Result.Success(new LoginResult(accessToken, rawRefresh, expiresAt));
    }

    // sub = UserId; role = claim JWT-native "role" = tên enum viết thường ("admin"/"staff"). DERIVE từ enum (KHÔNG ref
    // StarHill.Authorization — project đó FrameworkReference ASP.NET, ref vào Application sẽ phá I7). Hợp đồng
    // enum-name ↔ StarHillPolicies.RoleAdmin/RoleStaff được GUARD bằng test `RoleClaimContractTests` (fail build nếu lệch).
    private static ClaimsIdentity BuildIdentity(IdentityUser user)
    {
        var role = user.Role.ToString().ToLowerInvariant(); // Admin→"admin", Staff→"staff"
        return new ClaimsIdentity(
            [new Claim("sub", user.Id.ToString()), new Claim("role", role)],
            authenticationType: "jwt");
    }
}
