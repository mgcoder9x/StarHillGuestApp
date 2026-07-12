using Bedrock.Application.Ports.Persistence;
using Bedrock.Domain.Results;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

public sealed class TransactionTests
{
    [Fact]
    public async Task ExecuteInTransaction_rolls_back_all_on_error()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var id = Guid.CreateVersion7();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteInTransactionAsync<int>(async ct =>
                {
                    repo.Add(new TestThing { Name = "will-rollback" });
                    await uow.SaveChangesAsync(ct);
                    throw new InvalidOperationException("boom sau khi save trong transaction");
                }));
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(0, await db.Things.IgnoreQueryFilters().CountAsync());
        }
    }

    [Fact]
    public async Task ExecuteInTransaction_commits_on_success()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var result = await uow.ExecuteInTransactionAsync(async ct =>
            {
                repo.Add(new TestThing { Name = "committed" });
                await uow.SaveChangesAsync(ct);
                return 42;
            });

            Assert.Equal(42, result);
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(1, await db.Things.CountAsync());
        }
    }

    [Fact]
    public async Task ExecuteInTransaction_is_reentrant_nested_joins_current()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Gọi lồng: nếu mở transaction lồng thật thì SQLite/EF sẽ ném; join đúng thì chạy trơn.
            await uow.ExecuteInTransactionAsync(async outerCt =>
            {
                repo.Add(new TestThing { Name = "outer" });

                await uow.ExecuteInTransactionAsync(async innerCt =>
                {
                    repo.Add(new TestThing { Name = "inner" });
                    await uow.SaveChangesAsync(innerCt);
                    return 0;
                }, outerCt);

                await uow.SaveChangesAsync(outerCt);
                return 0;
            });
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(2, await db.Things.CountAsync()); // cả outer + inner commit cùng một transaction
        }
    }

    [Fact]
    public async Task Reentrant_nested_rollback_discards_outer_and_inner()
    {
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteInTransactionAsync<int>(async outerCt =>
                {
                    repo.Add(new TestThing { Name = "outer" });
                    await uow.SaveChangesAsync(outerCt);

                    await uow.ExecuteInTransactionAsync(async innerCt =>
                    {
                        repo.Add(new TestThing { Name = "inner" });
                        await uow.SaveChangesAsync(innerCt);
                        return 0;
                    }, outerCt);

                    // Lỗi sau khi inner "join" đã save → transaction ngoài rollback TẤT CẢ.
                    throw new InvalidOperationException("rollback ngoài");
                }));
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(0, await db.Things.IgnoreQueryFilters().CountAsync());
        }
    }

    [Fact]
    public async Task SaveChanges_maps_DbUpdateConcurrencyException_to_ConcurrencyConflictException()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Đánh dấu xóa một bản ghi CHƯA tồn tại → EF kỳ vọng 1 row, thực tế 0 → DbUpdateConcurrencyException.
        var ghost = new TestLog { Message = "ghost" };
        db.Logs.Attach(ghost);
        db.Entry(ghost).State = EntityState.Deleted;

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => uow.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_sync_is_blocked_to_avoid_skipping_dispatch()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        db.Things.Add(new TestThing { Name = "sync" });

        Assert.Throws<NotSupportedException>(() => db.SaveChanges());
    }
}
