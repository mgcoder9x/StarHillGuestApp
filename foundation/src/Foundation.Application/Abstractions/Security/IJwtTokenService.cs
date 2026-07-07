using System.Security.Claims;
using Foundation.SharedKernel.DependencyInjection;

namespace Foundation.Application.Abstractions.Security;

/// <summary>Access token đã phát: chuỗi JWT + thời điểm hết hạn (UTC).</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>Thông tin để phát access token (chủ thể + role + claim phụ tùy chọn).</summary>
public sealed record TokenIssueRequest(
    Guid UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyDictionary<string, string>? ExtraClaims = null);

/// <summary>
/// Phát &amp; xác minh JWT access token (HS256 ở MVP — đối xứng, backend vừa ký vừa verify).
/// Mở đường RS256/ES256 khi có bên thứ ba verify (đổi implementation, không đổi port này).
/// </summary>
public interface IJwtTokenService : ISingletonService
{
    AccessToken Issue(TokenIssueRequest request);

    /// <summary>Xác minh token; trả <see cref="ClaimsPrincipal"/> nếu hợp lệ, ngược lại <c>null</c> (không throw).</summary>
    ClaimsPrincipal? Validate(string token);
}
