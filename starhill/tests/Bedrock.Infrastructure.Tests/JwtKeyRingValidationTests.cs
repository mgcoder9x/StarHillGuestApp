using Bedrock.Application.Ports.Security;
using Bedrock.Infrastructure.Tokens;

namespace Bedrock.Infrastructure.Tests;

/// <summary>Task 9.2 — validate-on-start key-ring (F35, fail-fast). Truy cập internal qua InternalsVisibleTo.</summary>
public sealed class JwtKeyRingValidationTests
{
    private static byte[] Secret(byte seed) => [.. Enumerable.Repeat(seed, 32)];

    private static JwtKeyRingOptions Valid()
    {
        var options = new JwtKeyRingOptions
        {
            ActiveKid = "k1",
            Issuer = "iss",
            Audience = "aud",
            AccessTokenLifetimeMinutes = 15,
        };
        options.Keys.Add(new JwtSigningKey { Kid = "k1", Secret = Convert.ToBase64String(Secret(1)) });
        return options;
    }

    [Fact]
    public void Valid_keyring_passes()
    {
        JwtKeyRingValidation.Validate(Valid()); // không ném.
    }

    [Fact]
    public void Empty_keys_throws()
    {
        var options = Valid();
        options.Keys.Clear();

        Assert.Throws<InvalidOperationException>(() => JwtKeyRingValidation.Validate(options));
    }

    [Fact]
    public void Active_kid_not_in_keys_throws()
    {
        var options = Valid();
        options.ActiveKid = "does-not-exist";

        Assert.Throws<InvalidOperationException>(() => JwtKeyRingValidation.Validate(options));
    }

    [Fact]
    public void Short_secret_throws()
    {
        var options = new JwtKeyRingOptions { ActiveKid = "k1", Issuer = "iss", Audience = "aud", AccessTokenLifetimeMinutes = 15 };
        options.Keys.Add(new JwtSigningKey { Kid = "k1", Secret = Convert.ToBase64String([.. Enumerable.Repeat((byte)1, 16)]) }); // 128-bit < 256.

        Assert.Throws<InvalidOperationException>(() => JwtKeyRingValidation.Validate(options));
    }

    [Fact]
    public void Missing_issuer_or_audience_throws()
    {
        var options = Valid();
        options.Issuer = string.Empty;

        Assert.Throws<InvalidOperationException>(() => JwtKeyRingValidation.Validate(options));
    }

    [Fact]
    public void Non_base64_secret_throws()
    {
        var options = new JwtKeyRingOptions { ActiveKid = "k1", Issuer = "iss", Audience = "aud", AccessTokenLifetimeMinutes = 15 };
        options.Keys.Add(new JwtSigningKey { Kid = "k1", Secret = "not-base64!!!" });

        Assert.Throws<InvalidOperationException>(() => JwtKeyRingValidation.Validate(options));
    }
}
