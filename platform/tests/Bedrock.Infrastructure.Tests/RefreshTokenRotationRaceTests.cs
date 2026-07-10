using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL — task 8.3, CP7): rotation refresh-token đa-connection thật.
/// <list type="bullet">
///   <item>2 request đồng thời consume CÙNG token → ĐÚNG 1 thắng (UPDATE ... WHERE revoked_at IS NULL → row-lock).</item>
///   <item>consume + insert token mới trong 1 transaction: insert fail → consume ROLLBACK (không mất token gốc — F5).</item>
/// </list>
/// Dùng chung container qua <see cref="PostgresCollection"/>; skip nếu thiếu Docker (N-012).
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class RefreshTokenRotationRaceTests(PostgresFixture fixture)
{
    private async Task<ServiceProvider> BuildAsync(TestClock clock)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ICurrentUser>(new TestCurrentUser { UserId = Guid.CreateVersion7() });
        services.AddBedrockPersistence<PgOutboxDbContext>(o => o.UseNpgsql(fixture.Container.GetConnectionString()));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PgOutboxDbContext>();
            await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
            await db.Database
                .ExecuteSqlRawAsync("TRUNCATE TABLE outbox_message, inbox_message, states, refresh_token RESTART IDENTITY CASCADE")
                .ConfigureAwait(false);
        }

        return provider;
    }

    private static async Task<RefreshTokenSnapshot> SeedAsync(ServiceProvider provider, TestClock clock, string hash)
    {
        var snapshot = new RefreshTokenSnapshot(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), hash, clock.UtcNow.AddDays(30), null);
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await store.AddAsync(snapshot);
        await uow.SaveChangesAsync();
        return snapshot;
    }

    [SkippableFact]
    public async Task Two_concurrent_consumes_exactly_one_wins() // CP7
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(clock);
        var token = await SeedAsync(provider, clock, "hash-race");

        async Task<bool> ConsumeAsync()
        {
            await using var scope = provider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            return await store.TryConsumeAsync(token.Id, clock.UtcNow, "rotated", Guid.CreateVersion7());
        }

        var results = await Task.WhenAll(ConsumeAsync(), ConsumeAsync());

        Assert.Equal(1, results.Count(won => won)); // đúng MỘT request thắng (row-lock, không lock ứng dụng).
    }

    [SkippableFact]
    public async Task Consume_and_insert_roll_back_together_when_insert_conflicts() // F5 atomic (Postgres thật)
    {
        Skip.IfNot(fixture.Available, "Docker/Postgres không khả dụng — bỏ qua integration test.");
        var clock = new TestClock();
        await using var provider = await BuildAsync(clock);
        var token = await SeedAsync(provider, clock, "hash-original");
        await SeedAsync(provider, clock, "hash-dup"); // tồn tại sẵn → insert trùng hash sẽ vi phạm UNIQUE.

        await using (var scope = provider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var rotated = new RefreshTokenSnapshot(
                Guid.CreateVersion7(), token.UserId, token.FamilyId, "hash-dup", clock.UtcNow.AddDays(30), null);

            // consume (ExecuteUpdate trong transaction) + insert trùng hash → UNIQUE violation → rollback CẢ HAI.
            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    Assert.True(await store.TryConsumeAsync(token.Id, clock.UtcNow, "rotated", rotated.Id, ct));
                    await store.AddAsync(rotated, ct);
                    await uow.SaveChangesAsync(ct);
                    return 0;
                }));
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IRefreshTokenStore>();
            // Insert fail → consume rollback: token gốc VẪN chưa revoked (không mất token — §7.4/F5).
            Assert.Null((await store.GetByHashAsync("hash-original"))!.RevokedAt);
        }
    }
}
