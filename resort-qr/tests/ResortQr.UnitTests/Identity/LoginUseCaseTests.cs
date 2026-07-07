using System;
using System.Linq;
using System.Threading.Tasks;
using ResortQr.Application.Identity;
using ResortQr.Infrastructure.Security;
using Xunit;

namespace ResortQr.UnitTests.Identity;

public sealed class LoginUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Valid_credentials_should_issue_access_and_refresh()
    {
        var h = new AuthTestHarness(Now);
        var userId = h.AddUser("admin@x.com", "pw12345678");

        var result = await h.Login().ExecuteAsync(new LoginCommand("admin@x.com", "pw12345678"));

        Assert.True(result.IsSuccess);
        var tokens = result.Value;

        var principal = h.Jwt.Validate(tokens.AccessToken.Token);
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal!.FindFirst("sub")!.Value);

        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        var record = Assert.Single(h.RefreshTokens.Records);
        Assert.Null(record.RevokedAt);
        Assert.Equal(userId, record.UserId);
        Assert.Equal(h.RefreshHasher.Hash(tokens.RefreshToken), record.TokenHash);
        Assert.True(h.UnitOfWork.SaveChangesCallCount >= 1);
    }

    [Fact]
    public async Task Wrong_password_should_fail_and_not_issue_refresh()
    {
        var h = new AuthTestHarness(Now);
        h.AddUser("admin@x.com", "correct-password");

        var result = await h.Login().ExecuteAsync(new LoginCommand("admin@x.com", "wrong-password"));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_credentials", result.Error!.Code);
        Assert.Empty(h.RefreshTokens.Records);
    }

    [Fact]
    public async Task Unknown_email_should_fail_ambiguously()
    {
        var h = new AuthTestHarness(Now);
        h.AddUser("admin@x.com", "correct-password");

        var result = await h.Login().ExecuteAsync(new LoginCommand("ghost@x.com", "whatever12"));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_credentials", result.Error!.Code);
        Assert.Empty(h.RefreshTokens.Records);
    }

    [Fact]
    public async Task Inactive_user_should_fail()
    {
        var h = new AuthTestHarness(Now);
        h.AddUser("disabled@x.com", "pw12345678", active: false);

        var result = await h.Login().ExecuteAsync(new LoginCommand("disabled@x.com", "pw12345678"));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task Successful_login_with_weaker_hash_should_rehash_and_persist()
    {
        var h = new AuthTestHarness(Now); // login hasher: t=2
        var weakHasher = new Argon2idPasswordHasher(new PasswordHashingOptions
        {
            MemoryKib = 1024,
            Iterations = 1, // cũ hơn cấu hình hiện tại
            DegreeOfParallelism = 1,
            SaltSize = 16,
            HashSize = 32,
        });
        var userId = h.AddUserWithHash("admin@x.com", weakHasher.Hash("pw12345678"));

        var result = await h.Login().ExecuteAsync(new LoginCommand("admin@x.com", "pw12345678"));

        Assert.True(result.IsSuccess);
        var stored = h.Users.Get(userId)!;
        Assert.StartsWith("$argon2id$v=19$m=1024,t=2,p=1$", stored.PasswordHash, StringComparison.Ordinal);
    }
}
