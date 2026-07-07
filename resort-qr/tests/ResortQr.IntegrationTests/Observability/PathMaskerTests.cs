using ResortQr.Api.Observability;
using Xunit;

namespace ResortQr.IntegrationTests.Observability;

public sealed class PathMaskerTests
{
    [Theory]
    [InlineData("/r/secret-token-abc", "/r/***")]
    [InlineData("/api/guest/resolve/secret-token-xyz", "/api/guest/resolve/***")]
    public void Should_mask_token_paths(string input, string expected)
    {
        Assert.Equal(expected, PathMasker.Mask(input));
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/health/live")]
    public void Should_keep_non_token_paths(string input)
    {
        Assert.Equal(input, PathMasker.Mask(input));
    }

    [Fact]
    public void Should_return_empty_for_null_or_empty()
    {
        Assert.Equal(string.Empty, PathMasker.Mask(null));
        Assert.Equal(string.Empty, PathMasker.Mask(string.Empty));
    }
}
