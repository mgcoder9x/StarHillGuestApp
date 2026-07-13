using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Tiện ích test: cấu hình JWT key-ring + phát access-token mang claim <c>role</c> JWT-native (base
/// <c>RoleClaimType="role"</c>, <c>MapInboundClaims=false</c>). Dùng cho guard CP8 (superset Admin⊇Staff) +
/// guard ánh xạ role endpoint Rooms — KHÔNG cần DB/Docker (chỉ tầng authentication/authorization).
/// </summary>
internal static class JwtTestTokens
{
    public const string Kid = "k1";
    public const string Issuer = "test-issuer";
    public const string Audience = "test-audience";

    private static readonly byte[] SecretBytes = new byte[32]; // 256-bit đủ HS256 cho test.

    public static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ActiveKid"] = Kid,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Keys:0:Kid"] = Kid,
                ["Jwt:Keys:0:Secret"] = System.Convert.ToBase64String(SecretBytes),
            })
            .Build();

    /// <summary>Phát token; <paramref name="role"/> null → token hợp lệ nhưng KHÔNG có role (kiểm 403).</summary>
    public static string Issue(string? role)
    {
        var key = new SymmetricSecurityKey(SecretBytes) { KeyId = Kid };
        var claims = new Dictionary<string, object>
        {
            ["sub"] = System.Guid.CreateVersion7().ToString(),
        };
        if (role is not null)
        {
            claims["role"] = role;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = System.DateTime.UtcNow.AddMinutes(5),
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
