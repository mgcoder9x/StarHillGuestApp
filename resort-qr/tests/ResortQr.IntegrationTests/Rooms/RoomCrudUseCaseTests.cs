using System;
using System.Linq;
using System.Threading.Tasks;
using ResortQr.Application.Common;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using ResortQr.Infrastructure.Security;
using ResortQr.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ResortQr.IntegrationTests.Rooms;

/// <summary>Use-case CRUD còn lại (update/status/delete) + read-model EfRoomQueries (SQLite THẬT).</summary>
public sealed class RoomCrudUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<Guid> CreateRoomAsync(AppSqliteHarness h, string number)
    {
        await using var ctx = h.CreateContext();
        var uc = new CreateRoomUseCase(new EfUnitOfWork(ctx), h.CurrentUser, h.Clock, new CryptoTokenGenerator());
        return (await uc.ExecuteAsync(new CreateRoomInput(number, null, null))).Value.RoomId;
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "101");

        await using (var ctx = harness.CreateContext())
        {
            var result = await new UpdateRoomUseCase(new EfUnitOfWork(ctx))
                .ExecuteAsync(new UpdateRoomInput(roomId, "101B", "Tower", 5));
            Assert.True(result.IsSuccess);
        }

        await using (var verify = harness.CreateContext())
        {
            var room = await verify.Rooms.SingleAsync(r => r.Id == roomId);
            Assert.Equal("101B", room.RoomNumber);
            Assert.Equal("Tower", room.Building);
            Assert.Equal(5, room.Floor);
        }
    }

    [Fact]
    public async Task Update_to_existing_number_returns_validation_error()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        await CreateRoomAsync(harness, "201");
        var second = await CreateRoomAsync(harness, "202");

        await using var ctx = harness.CreateContext();
        var result = await new UpdateRoomUseCase(new EfUnitOfWork(ctx))
            .ExecuteAsync(new UpdateRoomInput(second, "201", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task Update_nonexistent_returns_not_found()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();

        await using var ctx = harness.CreateContext();
        var result = await new UpdateRoomUseCase(new EfUnitOfWork(ctx))
            .ExecuteAsync(new UpdateRoomInput(Guid.CreateVersion7(), "999", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task ChangeStatus_persists()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "301");

        await using (var ctx = harness.CreateContext())
        {
            var result = await new ChangeRoomStatusUseCase(new EfUnitOfWork(ctx))
                .ExecuteAsync(new ChangeRoomStatusInput(roomId, RoomStatus.Maintenance));
            Assert.True(result.IsSuccess);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.Equal(RoomStatus.Maintenance, (await verify.Rooms.SingleAsync(r => r.Id == roomId)).Status);
        }
    }

    [Fact]
    public async Task Delete_soft_deletes_and_keeps_tokens()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "401");

        await using (var ctx = harness.CreateContext())
        {
            var result = await new DeleteRoomUseCase(new EfUnitOfWork(ctx)).ExecuteAsync(roomId);
            Assert.True(result.IsSuccess);
        }

        await using (var verify = harness.CreateContext())
        {
            Assert.False(await verify.Rooms.AnyAsync(r => r.Id == roomId));
            var deleted = await verify.Rooms.IgnoreQueryFilters().SingleAsync(r => r.Id == roomId);
            Assert.True(deleted.IsDeleted);
            // Token giữ nguyên (không xóa cứng).
            Assert.True(await verify.RoomQrTokens.AnyAsync(t => t.RoomId == roomId));
        }
    }

    [Fact]
    public async Task Queries_list_excludes_soft_deleted_and_includes_active_token_preview()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var keep = await CreateRoomAsync(harness, "501");
        var drop = await CreateRoomAsync(harness, "502");

        await using (var ctx = harness.CreateContext())
        {
            await new DeleteRoomUseCase(new EfUnitOfWork(ctx)).ExecuteAsync(drop);
        }

        await using (var ctx = harness.CreateContext())
        {
            var queries = new EfRoomQueries(ctx);
            var page = await queries.ListAsync(new PagedRequest(1, 20));

            Assert.Equal(1, page.Total);
            var item = Assert.Single(page.Items);
            Assert.Equal(keep, item.Id);
            Assert.Equal("501", item.RoomNumber);
            Assert.False(string.IsNullOrEmpty(item.ActiveTokenPreview));
            Assert.EndsWith("…", item.ActiveTokenPreview!, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Queries_get_by_id_returns_null_for_unknown()
    {
        using var harness = new AppSqliteHarness(T0);
        await harness.SeedResortAsync();
        var roomId = await CreateRoomAsync(harness, "601");

        await using var ctx = harness.CreateContext();
        var queries = new EfRoomQueries(ctx);

        Assert.NotNull(await queries.GetByIdAsync(roomId));
        Assert.Null(await queries.GetByIdAsync(Guid.CreateVersion7()));
    }
}
