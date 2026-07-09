using System.Security.Claims;
using Bedrock.Application.Ports.Security;
using Bedrock.Infrastructure.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 9.2 — JWT key-ring signer. Verify bằng <see cref="JsonWebTokenHandler"/> với key-ring (self-contained,
/// không cần Api). Lifetime kiểm bằng <see cref="TestClock"/> (tất định). Bao gồm rolling rotation:
/// token ký bằng khóa cũ vẫn verify được khi ActiveKid đã đổi (khóa cũ còn trong Keys).
/// </summary>
public sealed class JwtTokenServiceTests
{
    private static byte[] Secret(byte seed) => [.. Enumerable.Repeat(seed, 32)]; // 256-bit HS256 key.

    private static JwtKeyRingOptions KeyRing(string activeKid, params (string Kid, byte[] Secret)[] keys)
    {
        var options = new JwtKeyRingOptions
        {
            ActiveKid = activeKid,
            Issuer = "bedrock-test-issuer",
            Audience = "bedrock-test-audience",
            AccessTokenLifetimeMinutes = 15,
        };
        foreach (var (kid, secret) in keys)
        {
            options.Keys.Add(new JwtSigningKey { Kid = kid, Secret = Convert.ToBase64String(secret) });
        }

        return options;
    }

    private static Task<TokenValidationResult> ValidateAsync(string token, JwtKeyRingOptions ring, TestClock clock)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ring.Issuer,
            ValidateAudience = true,
            ValidAudience = ring.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = ring.Keys.Select(k =>
                (SecurityKey)new SymmetricSecurityKey(Convert.FromBase64String(k.Secret)) { KeyId = k.Kid }),
            ValidateLifetime = true,
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = clock.UtcNow.UtcDateTime;
                return (notBefore is null || now >= notBefore) && (expires is null || now < expires);
            },
        };

        return new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
    }

    [Fact]
    public async Task Issue_produces_token_verifiable_with_kid_and_claims()
    {
        var clock = new TestClock();
        var ring = KeyRing("k1", ("k1", Secret(1)));
        var userId = Guid.CreateVersion7().ToString();

        var token = new JwtTokenService(ring, clock)
            .Issue(new ClaimsIdentity([new Claim("sub", userId), new Claim("role", "member")]));

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal("k1", parsed.Kid); // header kid = active key.

        var result = await ValidateAsync(token, ring, clock);
        Assert.True(result.IsValid);
        Assert.Equal(userId, result.ClaimsIdentity.FindFirst("sub")?.Value);
        Assert.Equal("member", result.ClaimsIdentity.FindFirst("role")?.Value); // tên claim JWT-native giữ nguyên (AD-023/AD-032).
    }

    [Fact]
    public async Task Token_signed_with_previous_key_still_verifies_after_rotation()
    {
        var clock = new TestClock();

        // Ký bằng k1 (active hiện tại).
        var signingRing = KeyRing("k1", ("k1", Secret(1)), ("k2", Secret(2)));
        var token = new JwtTokenService(signingRing, clock).Issue(new ClaimsIdentity([new Claim("sub", "u1")]));

        // Rotation: ActiveKid → k2, nhưng k1 VẪN trong Keys verify → token cũ vẫn hợp lệ (rolling rotation F22).
        var rotatedRing = KeyRing("k2", ("k1", Secret(1)), ("k2", Secret(2)));
        var result = await ValidateAsync(token, rotatedRing, clock);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Token_not_verifiable_when_signing_key_removed_from_ring()
    {
        var clock = new TestClock();
        var token = new JwtTokenService(KeyRing("k1", ("k1", Secret(1))), clock)
            .Issue(new ClaimsIdentity([new Claim("sub", "u1")]));

        // Key-ring verify KHÔNG còn k1 (đã retire hẳn) → token cũ không verify được nữa.
        var result = await ValidateAsync(token, KeyRing("k2", ("k2", Secret(2))), clock);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Ctor_throws_when_active_kid_not_in_keys()
    {
        var ring = KeyRing("missing", ("k1", Secret(1)));

        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(ring, new TestClock()));
    }

    [Fact]
    public void Issue_sets_expiry_from_configured_lifetime()
    {
        var ring = KeyRing("k1", ("k1", Secret(1)));
        ring.AccessTokenLifetimeMinutes = 20;

        var token = new JwtTokenService(ring, new TestClock()).Issue(new ClaimsIdentity([new Claim("sub", "u1")]));

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal(20, (int)Math.Round((parsed.ValidTo - parsed.IssuedAt).TotalMinutes));
    }
}
