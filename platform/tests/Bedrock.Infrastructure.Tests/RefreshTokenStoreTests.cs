using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 8.1/8.2 — refresh-token store nguyên tử (F5/F10/F19). Kiểm trên SQLite (DB quan hệ thật, không Docker):
/// consume-if-not-revoked, lookup theo hash trả cả token revoked (reuse-detection §7.4 — AD-031), revoke-family,
/// consume+insert all-or-nothing trong transaction, và ràng buộc UNIQUE hash (ux_refresh_hash — F10).
/// Race đa-connection thật (2 request đồng thời) → task 8.3 (Testcontainers, CP7).
/// </summary>
public sealed class RefreshTokenStoreTests
{
    [Fact]
    public async Task GetByHash_returns_token_and_null_when_absent()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var token = await SeedTokenAsync(harness);

        await using var scope = harness.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

        var found = await store.GetByHashAsync(token.TokenHash);
        Assert.NotNull(found);
        Assert.Equal(token.Id, found.Id);
        Assert.Equal(token.FamilyId, found.FamilyId);
        Assert.Null(found.RevokedAt);

        Assert.Null(await store.GetByHashAsync("no-such-hash"));
    }

    [Fact]
    public async Task GetByHash_returns_revoked_token_for_reuse_detection()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var token = await SeedTokenAsync(harness);

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            Assert.True(await store.TryConsumeAsync(token.Id, harness.Clock.UtcNow, "rotated", Guid.CreateVersion7()));
        }

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            var found = await store.GetByHashAsync(token.TokenHash);
            // Reuse-detection (§7.4) CHỈ hoạt động nếu lookup thấy được token đã revoked.
            Assert.NotNull(found);
            Assert.NotNull(found.RevokedAt);
        }
    }

    [Fact]
    public async Task TryConsume_succeeds_once_then_fails()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var token = await SeedTokenAsync(harness);
        var replacedBy = Guid.CreateVersion7();

        await using var scope = harness.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();

        Assert.True(await store.TryConsumeAsync(token.Id, harness.Clock.UtcNow, "rotated", replacedBy));
        // Lần 2 trên token đã revoked → 0 row → false (nền tảng chống race: chỉ 1 lời gọi thắng).
        Assert.False(await store.TryConsumeAsync(token.Id, harness.Clock.UtcNow, "rotated", replacedBy));
    }

    [Fact]
    public async Task RevokeFamily_revokes_all_active_tokens_in_family()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var family = Guid.CreateVersion7();
        var t1 = await SeedTokenAsync(harness, family);
        var t2 = await SeedTokenAsync(harness, family);
        var other = await SeedTokenAsync(harness); // family khác — KHÔNG bị đụng.

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            await store.RevokeFamilyAsync(family);
        }

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            Assert.NotNull((await store.GetByHashAsync(t1.TokenHash))!.RevokedAt);
            Assert.NotNull((await store.GetByHashAsync(t2.TokenHash))!.RevokedAt);
            Assert.Null((await store.GetByHashAsync(other.TokenHash))!.RevokedAt); // family khác giữ nguyên.
        }
    }

    [Fact]
    public async Task Consume_and_add_roll_back_together_on_error()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var token = await SeedTokenAsync(harness);
        var rotated = new RefreshTokenSnapshot(
            Guid.CreateVersion7(), token.UserId, token.FamilyId, "rotated-hash", harness.Clock.UtcNow.AddDays(30), null);

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    Assert.True(await store.TryConsumeAsync(token.Id, harness.Clock.UtcNow, "rotated", rotated.Id, ct));
                    await store.AddAsync(rotated, ct);
                    await uow.SaveChangesAsync(ct);
                    throw new InvalidOperationException("boom sau khi consume+insert");
                }));
        }

        await using (var scope = harness.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            // §7.4: insert fail → consume cũng rollback (không mất token gốc, không dư token mới).
            Assert.Null((await store.GetByHashAsync(token.TokenHash))!.RevokedAt);
            Assert.Null(await store.GetByHashAsync("rotated-hash"));
        }
    }

    [Fact]
    public async Task Duplicate_hash_is_rejected_by_unique_index()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var token = await SeedTokenAsync(harness);

        await using var scope = harness.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var duplicate = new RefreshTokenSnapshot(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            token.TokenHash, harness.Clock.UtcNow.AddDays(30), null); // TRÙNG hash.
        await store.AddAsync(duplicate);

        // F10: ux_refresh_hash UNIQUE → vi phạm là DbUpdateException (không phải concurrency).
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => uow.SaveChangesAsync());
    }

    private static async Task<RefreshTokenSnapshot> SeedTokenAsync(PersistenceHarness harness, Guid? familyId = null)
    {
        var snapshot = new RefreshTokenSnapshot(
            Id: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            FamilyId: familyId ?? Guid.CreateVersion7(),
            TokenHash: Guid.NewGuid().ToString("N"),
            ExpiresAt: harness.Clock.UtcNow.AddDays(30),
            RevokedAt: null);

        await using var scope = harness.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await store.AddAsync(snapshot);
        await uow.SaveChangesAsync();
        return snapshot;
    }
}
