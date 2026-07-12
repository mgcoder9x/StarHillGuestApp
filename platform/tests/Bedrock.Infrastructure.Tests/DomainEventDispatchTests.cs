using Bedrock.Application.Events;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// CP14 — hiệu ứng handler domain-event và state gốc commit CÙNG transaction; handler ném → không gì commit;
/// vượt MaxDispatchDepth → ném lỗi rõ; không handler → no-op.
/// </summary>
public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task Handler_effect_commits_in_same_transaction_as_state()
    {
        await using var harness = await PersistenceHarness.CreateAsync(services =>
            services.AddScoped<IDomainEventHandler<TestThingCreated>, SideEffectHandler>());

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var thing = new TestThing { Name = "with-event" };
            thing.EmitCreated();
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(1, await db.Things.CountAsync());
            Assert.Equal(1, await db.Logs.CountAsync()); // hiệu ứng handler đã commit cùng transaction
        }
    }

    [Fact]
    public async Task Handler_throwing_rolls_back_state_and_effects()
    {
        await using var harness = await PersistenceHarness.CreateAsync(services =>
            services.AddScoped<IDomainEventHandler<TestThingCreated>, ThrowingHandler>());

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var thing = new TestThing { Name = "with-event" };
            thing.EmitCreated();
            repo.Add(thing);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.SaveChangesAsync());
            Assert.Equal(ThrowingHandler.Message, ex.Message);
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(0, await db.Things.IgnoreQueryFilters().CountAsync()); // handler ném → state gốc KHÔNG commit
        }
    }

    [Fact]
    public async Task Domain_events_are_restored_after_handler_failure() // A-14: event KHÔNG mất → retry re-dispatch được
    {
        await using var harness = await PersistenceHarness.CreateAsync(services =>
            services.AddScoped<IDomainEventHandler<TestThingCreated>, ThrowingHandler>());

        await using var scope = harness.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var thing = new TestThing { Name = "with-event" };
        thing.EmitCreated();
        repo.Add(thing);

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.SaveChangesAsync());

        // Handler ném giữa chừng → dispatch loop KHÔI PHỤC event về entity (snapshot+restore) thay vì mất âm thầm →
        // lần SaveChanges sau (retry) còn dispatch lại được. Đây là behavior A-14 (clear-rồi-restore-khi-lỗi).
        Assert.NotEmpty(thing.DomainEvents);
    }

    [Fact]
    public async Task Exceeding_max_dispatch_depth_throws_clear_error()
    {
        await using var harness = await PersistenceHarness.CreateAsync(services =>
            services.AddScoped<IDomainEventHandler<ChainEvent>, ChainHandler>());

        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        db.MaxDepthOverride = 3;

        var seed = new TestThing { Name = "seed" };
        seed.EmitChain();
        repo.Add(seed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.SaveChangesAsync());
        Assert.Contains("vượt trần", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Domain_events_restored_when_convention_or_save_fails_after_dispatch() // P1-01/AD-094
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var thing = new TestThing { Name = "x" };
        thing.EmitCreated();
        db.Things.Add(thing);

        // Dispatch (KHÔNG dùng clock) chạy TRƯỚC + THÀNH CÔNG (dequeue+clear event); rồi ApplySoftDelete đọc clock →
        // NÉM = mô phỏng bước SAU dispatch (convention/base-save) thất bại.
        harness.Clock.Throw = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        // Failure boundary (AD-094) RESTORE event đã dequeue → KHÔNG mất (retry còn dispatch được). Code cũ (AD-075)
        // chỉ restore khi DISPATCH ném → test này sẽ FAIL trên code cũ (event bị mất khi convention/save ném sau dispatch).
        Assert.Single(thing.DomainEvents);
    }

    [Fact]
    public async Task No_handler_registered_is_noop()
    {
        // Không đăng ký handler nào cho TestThingCreated → dispatch no-op, save vẫn thành công.
        await using var harness = await PersistenceHarness.CreateAsync();

        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var thing = new TestThing { Name = "orphan-event" };
            thing.EmitCreated();
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Assert.Equal(1, await db.Things.CountAsync());
        }
    }
}
