using System;
using System.Text.RegularExpressions;
using ResortQr.Infrastructure.Security;
using Xunit;

namespace ResortQr.UnitTests.Infrastructure;

public sealed class CryptoTokenGeneratorTests
{
    private static readonly Regex Base64UrlOnly = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    private readonly CryptoTokenGenerator _sut = new();

    [Fact]
    public void Default_token_should_have_at_least_43_chars_256bit()
    {
        var token = _sut.NewToken();
        Assert.True(token.Length >= 43, $"Độ dài {token.Length} < 43.");
    }

    [Fact]
    public void Token_should_contain_only_base64url_chars()
    {
        for (var i = 0; i < 100; i++)
        {
            Assert.Matches(Base64UrlOnly, _sut.NewToken());
        }
    }

    [Fact]
    public void Tokens_should_be_distinct()
    {
        var a = _sut.NewToken();
        var b = _sut.NewToken();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ByteLength_below_minimum_should_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.NewToken(8));
    }
}
