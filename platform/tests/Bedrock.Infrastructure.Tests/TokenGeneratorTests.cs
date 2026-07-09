using Bedrock.Infrastructure.Tokens;

namespace Bedrock.Infrastructure.Tests;

/// <summary>Task 9.1 — CSPRNG token generator: base64url (URL-safe, không padding), đủ entropy, duy nhất.</summary>
public sealed class TokenGeneratorTests
{
    [Fact]
    public void NewToken_default_is_urlsafe_no_padding()
    {
        var token = new CryptoTokenGenerator().NewToken();

        // 32 byte → base64 44 ký tự có 1 '=' padding → trim = 43. URL-safe: không '+', '/', '='.
        Assert.Equal(43, token.Length);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void Two_tokens_differ()
    {
        var generator = new CryptoTokenGenerator();

        Assert.NotEqual(generator.NewToken(), generator.NewToken());
    }

    [Fact]
    public void NewToken_below_min_bytes_throws()
    {
        var generator = new CryptoTokenGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.NewToken(8));
    }
}
