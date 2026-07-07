using Foundation.Application.Abstractions.Security;

namespace Foundation.Application.Identity;

/// <summary>Projection người dùng cho xác thực (store trả về). KHÔNG phải domain entity của app.</summary>
public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string PasswordHash,
    IReadOnlyCollection<string> Roles,
    bool IsActive);

/// <summary>Bộ token trả về sau login/refresh: access JWT + refresh secret (raw, đặt vào cookie) + hạn refresh.</summary>
public sealed record AuthTokens(AccessToken AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public sealed record LoginCommand(string Email, string Password);

public sealed record RefreshCommand(string RefreshToken);

public sealed record LogoutCommand(string RefreshToken);
