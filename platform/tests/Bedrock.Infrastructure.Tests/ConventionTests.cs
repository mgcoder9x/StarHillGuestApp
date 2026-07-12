using Bedrock.Application.Ports.Persistence;
using Bedrock.Domain.Entities;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

public sealed class ConventionTests
{
    [Fact]
    public async Task Audit_set_CreatedAt_and_actor_on_add()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var createdAt = harness.Clock.UtcNow;

        Guid id;
        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var thing = new TestThing { Name = "a" };
            id = thing.Id;
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var loaded = await db.Things.SingleAsync(t => t.Id == id);
            Assert.Equal(createdAt, loaded.CreatedAt);
            Assert.Equal(harness.User.UserId, loaded.CreatedByUserId);
            Assert.Null(loaded.UpdatedAt);
            Assert.Null(loaded.UpdatedByUserId);
        }
    }

    [Fact]
    public async Task Audit_set_UpdatedAt_on_modify_without_touching_Created()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var createdAt = harness.Clock.UtcNow;

        Guid id;
        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var thing = new TestThing { Name = "a" };
            id = thing.Id;
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        var updatedAt = createdAt.AddMinutes(30);
        harness.Clock.UtcNow = updatedAt;

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var loaded = await db.Things.SingleAsync(t => t.Id == id);
            loaded.Name = "b";
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var loaded = await db.Things.SingleAsync(t => t.Id == id);
            Assert.Equal(createdAt, loaded.CreatedAt); // KHÔNG bị đụng
            Assert.Equal(updatedAt, loaded.UpdatedAt);
            Assert.Equal(harness.User.UserId, loaded.UpdatedByUserId);
        }
    }

    [Fact]
    public async Task Audit_created_metadata_is_protected_from_caller_tampering() // A-25
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var createdAt = harness.Clock.UtcNow;
        var originalActor = harness.User.UserId;

        Guid id;
        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var thing = new TestThing { Name = "a" };
            id = thing.Id;
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        harness.Clock.UtcNow = createdAt.AddHours(1);
        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var loaded = await db.Things.SingleAsync(t => t.Id == id);
            loaded.CreatedAt = createdAt.AddYears(-10);          // caller CỐ Ý tampering metadata tạo
            loaded.CreatedByUserId = Guid.CreateVersion7();
            loaded.Name = "b";
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var loaded = await db.Things.SingleAsync(t => t.Id == id);
            Assert.Equal(createdAt, loaded.CreatedAt);           // infrastructure sở hữu → KHÔNG bị caller ghi đè
            Assert.Equal(originalActor, loaded.CreatedByUserId); // KHÔNG bị caller ghi đè
            Assert.Equal("b", loaded.Name);                      // thay đổi hợp lệ vẫn persist
        }
    }

    [Fact]
    public async Task SoftDelete_marks_flag_and_query_filter_excludes()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        var deletedAt = harness.Clock.UtcNow;

        Guid id;
        await using (var scope = harness.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var thing = new TestThing { Name = "a" };
            id = thing.Id;
            repo.Add(thing);
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<TestThing>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var thing = await db.Things.SingleAsync(t => t.Id == id);
            repo.Remove(thing);
            await uow.SaveChangesAsync();
        }

        await using (var scope = harness.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            // Query filter loại bản ghi đã xóa mềm.
            Assert.False(await db.Things.AnyAsync(t => t.Id == id));

            // Bản ghi vẫn còn trong bảng, đánh dấu đúng (bỏ filter mới thấy).
            var raw = await db.Things.IgnoreQueryFilters().SingleAsync(t => t.Id == id);
            Assert.True(raw.IsDeleted);
            Assert.Equal(deletedAt, raw.DeletedAt);
        }
    }

    [Fact]
    public async Task SnakeCase_convention_applied_to_columns()
    {
        await using var harness = await PersistenceHarness.CreateAsync();
        await using var scope = harness.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var entityType = db.Model.FindEntityType(typeof(TestThing))!;
        var tableName = entityType.GetTableName()!;

        var columns = new List<string>();
        var connection = db.Database.GetDbConnection();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT name FROM pragma_table_info('{tableName}')";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        Assert.Contains("created_at", columns);
        Assert.Contains("created_by_user_id", columns);
        Assert.Contains("row_version", columns);
    }
}
