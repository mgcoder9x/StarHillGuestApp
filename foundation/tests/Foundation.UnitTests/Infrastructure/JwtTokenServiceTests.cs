using System;
using Foundation.Application.Abstractions.Security;
using Foundation.Infrastructure.Security;
using Foundation.UnitTests.TestDoubles;
using Xunit;

namespace Foundation.UnitTests.Infrastructure;

public sealed class JwtTokenServiceTests
{
    // Khóa ≥ 32 byte cho HS256 (chỉ dùng trong test).
    private const string TestKey = "test-signing-key-0123456789-abcdefghijklmnop";
    private static readonly string[] AdminStaffRoles = ["Admin", "Staff"];
    private static readonly DateTimeOffset T0 = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    private static JwtOptions Options() => new()
    {
        SigningKey = TestKey,
        Issuer = "foundation-tests",
        Audience = "foundation-clients",
        AccessTokenMinutes = 15,
    };

    [Fact]
    public void Issue_then_validate_should_roundtrip_subject_and_roles()
    {
        var clock = new FakeDateTimeProvider(T0);
        var sut = new JwtTokenService(Options(), clock);
        var userId = Guid.CreateVersion7();

        var issued = sut.Issue(new TokenIssueRequest(userId, AdminStaffRoles));

        Assert.False(string.IsNullOrWhiteSpace(issued.Token));
        Assert.Equal(T0.AddMinutes(15), issued.ExpiresAt);

        var principal = sut.Validate(issued.Token);

        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal!.FindFirst("sub")!.Value);
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("Staff"));
        Assert.False(principal.IsInRole("Nope"));
    }

    [Fact]
    public void Validate_tampered_token_should_return_null()
    {
        var sut = new JwtTokenService(Options(), new FakeDateTimeProvider(T0));
        var issued = sut.Issue(new TokenIssueRequest(Guid.CreateVersion7(), Array.Empty<string>()));

        var tampered = issued.Token[..^2] + (issued.Token[^1] == 'a' ? "bb" : "aa");

        Assert.Null(sut.Validate(tampered));
    }

    [Fact]
    public void Validate_after_expiry_should_return_null()
    {
        var clock = new FakeDateTimeProvider(T0);
        var sut = new JwtTokenService(Options(), clock);
        var issued = sut.Issue(new TokenIssueRequest(Guid.CreateVersion7(), Array.Empty<string>()));

        clock.UtcNow = T0.AddMinutes(16); // vượt hạn 15'

        Assert.Null(sut.Validate(issued.Token));
    }

    [Fact]
    public void Validate_wrong_issuer_should_return_null()
    {
        var clock = new FakeDateTimeProvider(T0);
        var issuer = new JwtTokenService(Options(), clock);
        var issued = issuer.Issue(new TokenIssueRequest(Guid.CreateVersion7(), Array.Empty<string>()));

        var otherOptions = Options();
        otherOptions.Issuer = "different-issuer";
        var validator = new JwtTokenService(otherOptions, clock);

        Assert.Null(validator.Validate(issued.Token));
    }

    [Fact]
    public void ExtraClaims_should_be_present_after_validate()
    {
        var sut = new JwtTokenService(Options(), new FakeDateTimeProvider(T0));
        var extra = new Dictionary<string, string> { ["tenant"] = "starhill" };

        var issued = sut.Issue(new TokenIssueRequest(Guid.CreateVersion7(), Array.Empty<string>(), extra));
        var principal = sut.Validate(issued.Token);

        Assert.Equal("starhill", principal!.FindFirst("tenant")!.Value);
    }
}
