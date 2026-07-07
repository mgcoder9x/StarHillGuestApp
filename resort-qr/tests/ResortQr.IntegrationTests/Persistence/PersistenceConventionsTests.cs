using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Kiểm các convention nền của <c>ResortQrDbContext</c> trên SQLite thật (Docker-free):
/// audit tự động, xóa mềm (filter + Delete→Modified), snake_case (EFCore.NamingConventions trên EF10).
/// </summary>
public sealed class PersistenceConventionsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Adding_entity_sets_created_audit_fields()
    {
        var actor = Guid.CreateVersion7();
        using var harness = new SqlitePersistenceHarness(T0, actor);

        Guid id;
        await using (var ctx = harness.CreateContext())
        {
            var entity = new SampleEntity { Name = "alpha" };
            id = entity.Id;
            ctx.Samples.Add(entity);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var loaded = await ctx.Samples.SingleAsync(e => e.Id == id);
            Assert.Equal(T0, loaded.CreatedAt);
            Assert.Equal(actor, loaded.CreatedByUserId);
            Assert.Null(loaded.UpdatedAt);
            Assert.Null(loaded.UpdatedByUserId);
        }
    }

    [Fact]
    public async Task Updating_entity_sets_updated_fields_without_touching_created()
    {
        var actor = Guid.CreateVersion7();
        using var harness = new SqlitePersistenceHarness(T0, actor);

        Guid id;
        await using (var ctx = harness.CreateContext())
        {
            var entity = new SampleEntity { Name = "alpha" };
            id = entity.Id;
            ctx.Samples.Add(entity);
            await ctx.SaveChangesAsync();
        }

        // Đẩy thời gian rồi cập nhật.
        harness.Clock.UtcNow = T0.AddHours(3);
        await using (var ctx = harness.CreateContext())
        {
            var loaded = await ctx.Samples.SingleAsync(e => e.Id == id);
            loaded.Name = "beta";
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            var loaded = await ctx.Samples.SingleAsync(e => e.Id == id);
            Assert.Equal("beta", loaded.Name);
            Assert.Equal(T0, loaded.CreatedAt);              // KHÔNG bị ghi đè
            Assert.Equal(actor, loaded.CreatedByUserId);      // KHÔNG bị ghi đè
            Assert.Equal(T0.AddHours(3), loaded.UpdatedAt);
            Assert.Equal(actor, loaded.UpdatedByUserId);
        }
    }

    [Fact]
    public async Task Removing_soft_deletable_marks_deleted_and_hides_from_default_query()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);

        Guid id;
        await using (var ctx = harness.CreateContext())
        {
            var entity = new SampleEntity { Name = "to-delete" };
            id = entity.Id;
            ctx.Samples.Add(entity);
            await ctx.SaveChangesAsync();
        }

        harness.Clock.UtcNow = T0.AddMinutes(10);
        await using (var ctx = harness.CreateContext())
        {
            var loaded = await ctx.Samples.SingleAsync(e => e.Id == id);
            ctx.Samples.Remove(loaded);           // Delete → convention chuyển thành xóa mềm
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = harness.CreateContext())
        {
            // Query mặc định bị lọc bởi HasQueryFilter(!IsDeleted).
            Assert.False(await ctx.Samples.AnyAsync(e => e.Id == id));

            // Bản ghi VẪN còn trong DB (xóa mềm) — bỏ filter thì thấy, IsDeleted=true + DeletedAt set.
            var raw = await ctx.Samples.IgnoreQueryFilters().SingleAsync(e => e.Id == id);
            Assert.True(raw.IsDeleted);
            Assert.Equal(T0.AddMinutes(10), raw.DeletedAt);
        }
    }

    [Fact]
    public async Task Snake_case_naming_applies_to_refresh_token_table_and_columns()
    {
        using var harness = new SqlitePersistenceHarness(T0, actor: null);
        await using var ctx = harness.CreateContext();

        var columns = await GetColumnNamesAsync(ctx, "refresh_token");

        // Tên bảng snake_case (ToTable) + cột snake_case (convention): token_hash, user_id, family_id...
        Assert.Contains("token_hash", columns);
        Assert.Contains("user_id", columns);
        Assert.Contains("family_id", columns);
        Assert.Contains("expires_at", columns);
        Assert.Contains("revoked_at", columns);
        Assert.Contains("replaced_by_token_id", columns);
    }

    private static async Task<string[]> GetColumnNamesAsync(SampleDbContext ctx, string table)
    {
        var connection = ctx.Database.GetDbConnection();
        await ctx.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";
        var names = new System.Collections.Generic.List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return [.. names];
    }
}
