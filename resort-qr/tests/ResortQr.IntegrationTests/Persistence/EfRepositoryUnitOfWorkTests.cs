using System;
using System.Threading.Tasks;
using ResortQr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Kiểm <see cref="EfRepository{T}"/> + <see cref="EfUnitOfWork"/> trên SQLite thật:
/// repository chỉ stage (không tự ghi), UoW là điểm ghi; transaction all-or-nothing (rollback khi lỗi).
/// </summary>
public sealed class EfRepositoryUnitOfWorkTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Repository_add_is_persisted_only_after_unit_of_work_save()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);

        Guid id;
        await using (var ctx = harness.CreateContext())
        {
            var uow = new EfUnitOfWork(ctx);
            var entity = new SampleEntity { Name = "x" };
            id = entity.Id;

            uow.Repository<SampleEntity>().Add(entity);

            // Chưa SaveChanges → context KHÁC (cùng DB) chưa thấy.
            await using (var probe = harness.CreateContext())
            {
                Assert.False(await probe.Samples.AnyAsync(e => e.Id == id));
            }

            await uow.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            Assert.True(await ctx.Samples.AnyAsync(e => e.Id == id));
        }
    }

    [Fact]
    public async Task ExecuteInTransaction_rolls_back_all_changes_on_failure()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);

        var id = Guid.CreateVersion7();
        await using (var ctx = harness.CreateContext())
        {
            var uow = new EfUnitOfWork(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    ctx.Samples.Add(new SampleEntity { Id = id, Name = "will-rollback" });
                    await ctx.SaveChangesAsync(ct);

                    // Lỗi nghiệp vụ giữa transaction → toàn bộ phải rollback.
                    throw new InvalidOperationException("boom");
                }));
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.False(await verify.Samples.AnyAsync(e => e.Id == id));
        }
    }

    [Fact]
    public async Task ExecuteInTransaction_commits_when_action_succeeds()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);

        var id = Guid.CreateVersion7();
        await using (var ctx = harness.CreateContext())
        {
            var uow = new EfUnitOfWork(ctx);

            var affected = await uow.ExecuteInTransactionAsync(async ct =>
            {
                ctx.Samples.Add(new SampleEntity { Id = id, Name = "committed" });
                return await ctx.SaveChangesAsync(ct);
            });

            Assert.Equal(1, affected);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.True(await verify.Samples.AnyAsync(e => e.Id == id));
        }
    }
}
