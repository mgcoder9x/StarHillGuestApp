using System.Security.Claims;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bedrock.Infrastructure.Tokens;

/// <summary>
/// Impl <see cref="IJwtTokenService"/> — ký JWT HS256 bằng khóa <c>ActiveKid</c> của key-ring (F22), gắn
/// <c>kid</c> vào header để phía verify (<c>Bedrock.Api</c>) chọn đúng khóa. Dùng
/// <see cref="JsonWebTokenHandler"/> (bản Microsoft khuyến nghị) — KHÔNG dùng legacy <c>JwtSecurityTokenHandler</c>
/// vì handler cũ có <c>DefaultOutboundClaimTypeMap</c> tĩnh (global mutable) có thể remap tên claim khi ký, đe doạ
/// tên claim JWT-native đã chốt ở AD-023 (AD-032). Thời gian lấy từ <see cref="IClock"/> (tất định, test được).
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtKeyRingOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(JwtKeyRingOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options;
        _clock = clock;

        var activeKey = options.Keys.FirstOrDefault(k => string.Equals(k.Kid, options.ActiveKid, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"JwtKeyRingOptions: ActiveKid '{options.ActiveKid}' không có trong Keys — không thể ký (F22/F35).");

        var key = new SymmetricSecurityKey(Convert.FromBase64String(activeKey.Secret)) { KeyId = activeKey.Kid };
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string Issue(ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var now = _clock.UtcNow.UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = identity, // business claims (sub/role/permission/tenant_id/sid) do module dựng — AD-023.
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes),
            SigningCredentials = _signingCredentials, // KeyId set → JsonWebTokenHandler tự gắn "kid" vào header.
        };

        return Handler.CreateToken(descriptor);
    }
}
