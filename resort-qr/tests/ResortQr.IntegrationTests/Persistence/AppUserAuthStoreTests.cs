using System;
using System.Threading.Tasks;
using ResortQr.Domain.Identity;
using ResortQr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>Kiểm <see cref="AppUserAuthStore"/> trên SQLite THẬT: lookup + cập nhật hash (stage, commit qua Save).</summary>
public sealed class AppUserAuthStoreTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Find_by_email_and_id_returns_projected_user()
    {
        using var harness = new AppSqliteHarness(T0);
        var (userId, _) = await SeedUserAsync(harness, "admin@x.com", UserRole.Admin);

        await using var ctx = harness.CreateContext();
        var store = new AppUserAuthStore(ctx);

        var byEmail = await store.FindByEmailAsync("admin@x.com");
        Assert.NotNull(byEmail);
        Assert.Equal(userId, byEmail!.UserId);
        Assert.Contains("Admin", byEmail.Roles);
        Assert.True(byEmail.IsActive);

        var byId = await store.FindByIdAsync(userId);
        Assert.NotNull(byId);
        Assert.Equal("admin@x.com", byId!.Email);
    }

    [Fact]
    public async Task Find_by_email_unknown_returns_null()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        await using var ctx = harness.CreateContext();
        var store = new AppUserAuthStore(ctx);

        Assert.Null(await store.FindByEmailAsync("nobody@x.com"));
    }

    [Fact]
    public async Task Update_password_hash_stages_and_commits_via_savechanges()
    {
        using var harness = new AppSqliteHarness(T0);
        var (userId, _) = await SeedUserAsync(harness, "u@x.com", UserRole.Staff);

        await using (var ctx = harness.CreateContext())
        {
            var store = new AppUserAuthStore(ctx);
            await store.UpdatePasswordHashAsync(userId, "new-hash");
            await ctx.SaveChangesAsync();
        }

        await using (var verify = harness.CreateContext())
        {
            var user = await verify.AppUsers.SingleAsync(u => u.Id == userId);
            Assert.Equal("new-hash", user.PasswordHash);
        }
    }

    private static async Task<(Guid UserId, Guid ResortId)> SeedUserAsync(AppSqliteHarness harness, string email, UserRole role)
    {
        var resortId = await harness.SeedResortAsync();
        await using var ctx = harness.CreateContext();
        var user = new AppUser
        {
            ResortId = resortId,
            Email = email,
            DisplayName = "User",
            PasswordHash = "old-hash",
            Role = role,
            IsActive = true,
        };
        ctx.AppUsers.Add(user);
        await ctx.SaveChangesAsync();
        return (user.Id, resortId);
    }
}
