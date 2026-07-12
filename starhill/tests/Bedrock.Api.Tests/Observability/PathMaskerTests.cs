using Bedrock.Api.Observability;
using Xunit;

namespace Bedrock.Api.Tests.Observability;

public sealed class PathMaskerTests
{
    private static PathMasker Create(params string[] prefixes)
    {
        var options = new ObservabilityOptions();
        foreach (var p in prefixes)
        {
            options.MaskedPathPrefixes.Add(p);
        }

        return new PathMasker(options);
    }

    [Fact]
    public void Should_mask_tail_after_sensitive_prefix()
    {
        var masker = Create("/r/", "/api/guest/resolve/");

        Assert.Equal("/r/***", masker.Mask("/r/SECRET-TOKEN-123"));
        Assert.Equal("/api/guest/resolve/***", masker.Mask("/api/guest/resolve/abc.def.ghi"));
    }

    [Fact]
    public void Should_leave_non_sensitive_path_unchanged()
    {
        var masker = Create("/r/");

        Assert.Equal("/api/rooms/42", masker.Mask("/api/rooms/42"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Should_normalize_null_or_empty_to_empty(string? path)
    {
        var masker = Create("/r/");

        Assert.Equal(string.Empty, masker.Mask(path));
    }

    [Fact]
    public void Should_match_prefix_case_insensitively()
    {
        var masker = Create("/r/");

        Assert.Equal("/r/***", masker.Mask("/R/TOKEN"));
    }

    [Fact]
    public void No_prefixes_configured_should_pass_through()
    {
        var masker = Create();

        Assert.Equal("/r/token", masker.Mask("/r/token"));
    }

    [Fact]
    public void Custom_placeholder_should_be_used()
    {
        var options = new ObservabilityOptions { MaskPlaceholder = "[redacted]" };
        options.MaskedPathPrefixes.Add("/r/");
        var masker = new PathMasker(options);

        Assert.Equal("/r/[redacted]", masker.Mask("/r/token"));
    }
}
