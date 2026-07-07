using System;
using System.Threading.Tasks;
using ResortQr.Application.Identity;
using ResortQr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Kiểm <see cref="EfRefreshTokenStore"/> trên SQLite thật (Docker-free): rotation atomic consume-if-not-revoked
/// (fix expert #1 tại tầng DB thật), reuse/logout, revoke-family. Ghi rõ: race ĐA-CONNECTION thật (2 kết nối
/// đồng thời) cần Postgres/Testcontainers — SQLite in-memory 1 connection kiểm ĐÚNG ĐẮN ngữ nghĩa SQL
/// (UPDATE ... WHERE revoked_at IS NULL → lần 2 ảnh hưởng 0 row), là bất biến cốt lõi của cơ chế.
/// </summary>
public sealed class EfRefreshTokenStoreTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private static RefreshTokenRecord NewRecord(Guid userId, Guid familyId, string hash) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        FamilyId = familyId,
        TokenHash = hash,
        CreatedAt = T0,
        ExpiresAt = T0.AddDays(30),
    };

    [Fact]
    public async Task Add_then_find_by_hash_returns_record()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        var record = NewRecord(Guid.CreateVersion7(), Guid.CreateVersion7(), "hash-a");

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            await store.AddAsync(record);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            var found = await store.FindByHashAsync("hash-a");
            Assert.NotNull(found);
            Assert.Equal(record.Id, found!.Id);
        }
    }

    [Fact]
    public async Task TryConsume_succeeds_once_then_fails_second_time()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        var record = NewRecord(Guid.CreateVersion7(), Guid.CreateVersion7(), "hash-b");

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            await store.AddAsync(record);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);

            var first = await store.TryConsumeAsync(record.Id, T0.AddMinutes(1), "rotated", Guid.CreateVersion7());
            var second = await store.TryConsumeAsync(record.Id, T0.AddMinutes(2), "rotated", Guid.CreateVersion7());

            Assert.True(first);   // chính lời gọi này thu hồi
            Assert.False(second); // đã revoked → consume-if-not-revoked trả false (race/reuse)
        }

        await using (var ctx = harness.CreateContext())
        {
            var revoked = await ctx.RefreshTokens.SingleAsync(r => r.Id == record.Id);
            Assert.Equal(T0.AddMinutes(1), revoked.RevokedAt);   // giữ mốc của lần THẮNG (lần 1)
            Assert.Equal("rotated", revoked.RevokedReason);
        }
    }

    [Fact]
    public async Task TryConsume_on_unknown_token_returns_false()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        await using var ctx = harness.CreateContext();
        var store = new EfRefreshTokenStore(ctx);

        var consumed = await store.TryConsumeAsync(Guid.CreateVersion7(), T0, "rotated", Guid.CreateVersion7());

        Assert.False(consumed);
    }

    [Fact]
    public async Task RevokeFamily_revokes_only_active_tokens_in_family()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        var family = Guid.CreateVersion7();
        var otherFamily = Guid.CreateVersion7();
        var user = Guid.CreateVersion7();

        var active1 = NewRecord(user, family, "f-active-1");
        var active2 = NewRecord(user, family, "f-active-2");
        var alreadyRevoked = NewRecord(user, family, "f-revoked");
        alreadyRevoked.RevokedAt = T0.AddMinutes(-5);
        alreadyRevoked.RevokedReason = "logout";
        var outsider = NewRecord(user, otherFamily, "other-family");

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            await store.AddAsync(active1);
            await store.AddAsync(active2);
            await store.AddAsync(alreadyRevoked);
            await store.AddAsync(outsider);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            await store.RevokeFamilyAsync(family, T0.AddMinutes(1), "reuse_detected");
        }

        await using (var ctx = harness.CreateContext())
        {
            // 2 token active của family → revoked với lý do mới.
            Assert.Equal(T0.AddMinutes(1), (await ctx.RefreshTokens.SingleAsync(r => r.Id == active1.Id)).RevokedAt);
            Assert.Equal(T0.AddMinutes(1), (await ctx.RefreshTokens.SingleAsync(r => r.Id == active2.Id)).RevokedAt);

            // Token đã revoked trước đó KHÔNG bị ghi đè mốc/lý do (WHERE revoked_at IS NULL).
            var untouched = await ctx.RefreshTokens.SingleAsync(r => r.Id == alreadyRevoked.Id);
            Assert.Equal(T0.AddMinutes(-5), untouched.RevokedAt);
            Assert.Equal("logout", untouched.RevokedReason);

            // Family khác KHÔNG bị đụng.
            Assert.Null((await ctx.RefreshTokens.SingleAsync(r => r.Id == outsider.Id)).RevokedAt);
        }
    }

    [Fact]
    public async Task Logout_pattern_find_mutate_update_save_persists_revocation()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        var record = NewRecord(Guid.CreateVersion7(), Guid.CreateVersion7(), "hash-logout");

        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            await store.AddAsync(record);
            await ctx.SaveChangesAsync();
        }

        // Mô phỏng LogoutUseCase: FindByHash (tracked) → mutate → UpdateAsync → SaveChanges.
        await using (var ctx = harness.CreateContext())
        {
            var store = new EfRefreshTokenStore(ctx);
            var found = await store.FindByHashAsync("hash-logout");
            Assert.NotNull(found);
            found!.RevokedAt = T0.AddMinutes(5);
            found.RevokedReason = "logout";
            await store.UpdateAsync(found);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var reloaded = await ctx.RefreshTokens.SingleAsync(r => r.Id == record.Id);
            Assert.Equal(T0.AddMinutes(5), reloaded.RevokedAt);
            Assert.Equal("logout", reloaded.RevokedReason);
        }
    }
}
