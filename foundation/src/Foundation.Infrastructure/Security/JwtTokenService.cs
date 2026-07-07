using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Foundation.Application.Abstractions;
using Foundation.Application.Abstractions.Security;
using Microsoft.IdentityModel.Tokens;

namespace Foundation.Infrastructure.Security;

/// <summary>
/// Phát &amp; xác minh JWT HS256 (đối xứng). Claim: <c>sub</c> = userId, <c>role</c> = vai trò, <c>jti</c> = id token.
/// Thời gian lấy từ <see cref="IDateTimeProvider"/> (tất định, test được). ClockSkew = 0 (hết hạn dứt khoát).
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private const string RoleClaim = "role";

    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(JwtOptions options, IDateTimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _options = options;
        _clock = clock;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    }

    public AccessToken Issue(TokenIssueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        foreach (var role in request.Roles)
        {
            claims.Add(new Claim(RoleClaim, role));
        }

        if (request.ExtraClaims is not null)
        {
            foreach (var kv in request.ExtraClaims)
            {
                claims.Add(new Claim(kv.Key, kv.Value));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return new AccessToken(handler.WriteToken(token), expires);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Kiểm hạn theo clock được INJECT (tất định, không phụ thuộc giờ máy). Khi set LifetimeValidator,
            // nó thay thế kiểm lifetime mặc định → test/refresh dùng cùng nguồn thời gian với phần còn lại.
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = _clock.UtcNow.UtcDateTime;
                if (notBefore.HasValue && now < notBefore.Value)
                {
                    return false;
                }

                return !expires.HasValue || now < expires.Value;
            },
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = RoleClaim,
        };

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
