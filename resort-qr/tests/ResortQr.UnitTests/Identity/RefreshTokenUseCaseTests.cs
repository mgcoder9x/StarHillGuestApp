using System;
using System.Linq;
using System.Threading.Tasks;
using ResortQr.Application.Identity;
using Xunit;

namespace ResortQr.UnitTests.Identity;

public sealed class RefreshTokenUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 10, 0, 0, TimeSpan.Zero);

    private static async Task<(AuthTestHarness Harness, AuthTokens Tokens)> LoggedInAsync()
    {
        var h = new AuthTestHarness(Now);
        h.AddUser("admin@x.com", "pw12345678");
        var login = await h.Login().ExecuteAsync(new LoginCommand("admin@x.com", "pw12345678"));
        return (h, login.Value);
    }

    [Fact]
    public async Task Valid_refresh_should_rotate_and_revoke_old()
    {
        var (h, tokens) = await LoggedInAsync();

        var result = await h.Refresh().ExecuteAsync(new RefreshCommand(tokens.RefreshToken));

        Assert.True(result.IsSuccess);
        var rotated = result.Value;
        Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);

        var oldRecord = h.RefreshTokens.GetByHash(h.RefreshHasher.Hash(tokens.RefreshToken))!;
        Assert.NotNull(oldRecord.RevokedAt);
        Assert.NotNull(oldRecord.ReplacedByTokenId);
        Assert.Equal("rotated", oldRecord.RevokedReason);

        var newRecord = h.RefreshTokens.GetByHash(h.RefreshHasher.Hash(rotated.RefreshToken))!;
        Assert.Null(newRecord.RevokedAt);
        Assert.Equal(oldRecord.FamilyId, newRecord.FamilyId);
    }

    [Fact]
    public async Task Reusing_rotated_token_should_revoke_entire_family()
    {
        var (h, tokens) = await LoggedInAsync();

        var rotated = (await h.Refresh().ExecuteAsync(new RefreshCommand(tokens.RefreshToken))).Value;

        // Trình lại token CŨ (đã xoay) → reuse detection.
        var reuse = await h.Refresh().ExecuteAsync(new RefreshCommand(tokens.RefreshToken));

        Assert.True(reuse.IsFailure);
        Assert.Equal("invalid_refresh_token", reuse.Error!.Code);

        // Toàn bộ family bị thu hồi → token mới cũng vô hiệu.
        Assert.All(h.RefreshTokens.Records, r => Assert.NotNull(r.RevokedAt));

        var afterFamilyRevoke = await h.Refresh().ExecuteAsync(new RefreshCommand(rotated.RefreshToken));
        Assert.True(afterFamilyRevoke.IsFailure);
    }

    [Fact]
    public async Task Expired_refresh_should_fail()
    {
        var (h, tokens) = await LoggedInAsync();
        h.Clock.UtcNow = Now.AddDays(31); // quá hạn 30 ngày

        var result = await h.Refresh().ExecuteAsync(new RefreshCommand(tokens.RefreshToken));

        Assert.True(result.IsFailure);
        Assert.Equal("refresh_token_expired", result.Error!.Code);
    }

    [Fact]
    public async Task Unknown_refresh_token_should_fail()
    {
        var (h, _) = await LoggedInAsync();

        var result = await h.Refresh().ExecuteAsync(new RefreshCommand("this-token-does-not-exist"));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_refresh_token", result.Error!.Code);
    }

    [Fact] // Chống race: consume nguyên tử → chỉ MỘT lời gọi thắng (fix expert #1)
    public async Task Atomic_consume_should_let_only_one_caller_win()
    {
        var (h, tokens) = await LoggedInAsync();
        var record = h.RefreshTokens.GetByHash(h.RefreshHasher.Hash(tokens.RefreshToken))!;

        var first = await h.RefreshTokens.TryConsumeAsync(record.Id, Now, "rotated", Guid.CreateVersion7());
        var second = await h.RefreshTokens.TryConsumeAsync(record.Id, Now, "rotated", Guid.CreateVersion7());

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task Logout_should_revoke_current_token()
    {
        var (h, tokens) = await LoggedInAsync();

        var logout = await h.Logout().ExecuteAsync(new LogoutCommand(tokens.RefreshToken));
        Assert.True(logout.IsSuccess);

        var record = h.RefreshTokens.GetByHash(h.RefreshHasher.Hash(tokens.RefreshToken))!;
        Assert.NotNull(record.RevokedAt);
        Assert.Equal("logout", record.RevokedReason);
    }
}
