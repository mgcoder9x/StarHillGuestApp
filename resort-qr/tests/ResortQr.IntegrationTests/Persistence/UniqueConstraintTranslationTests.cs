using System;
using System.Threading.Tasks;
using ResortQr.Domain.Rooms;
using ResortQr.Infrastructure.Persistence;
using ResortQr.SharedKernel.Results;
using Xunit;

namespace ResortQr.IntegrationTests.Persistence;

/// <summary>
/// Fix gốc DEC-056: EfUnitOfWork dịch unique-violation của provider → <see cref="UniqueConstraintViolationException"/>
/// TRUNG LẬP (thay vì DbUpdateException thô). Verify trên SQLite THẬT.
/// </summary>
public sealed class UniqueConstraintTranslationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Duplicate_room_number_via_unitofwork_throws_neutral_unique_exception()
    {
        using var harness = new AppSqliteHarness(T0);
        var resortId = await harness.SeedResortAsync();
        await harness.SeedRoomAsync(resortId, "101");

        await using var ctx = harness.CreateContext();
        var uow = new EfUnitOfWork(ctx);
        uow.Repository<Room>().Add(new Room { ResortId = resortId, RoomNumber = "101" });

        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => uow.SaveChangesAsync());
    }

    [Fact]
    public async Task Non_unique_db_error_is_not_swallowed_as_unique_violation()
    {
        using var harness = new AppSqliteHarness(T0);

        await using var ctx = harness.CreateContext();
        var uow = new EfUnitOfWork(ctx);
        // FK vi phạm (resort_id không tồn tại) — KHÔNG phải unique-violation → KHÔNG map thành UniqueConstraintViolationException.
        uow.Repository<Room>().Add(new Room { ResortId = Guid.CreateVersion7(), RoomNumber = "9" });

        await Assert.ThrowsAnyAsync<Exception>(() => uow.SaveChangesAsync());
        // Cụ thể: KHÔNG phải UniqueConstraintViolationException.
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
        {
            await using var ctx2 = harness.CreateContext();
            var uow2 = new EfUnitOfWork(ctx2);
            uow2.Repository<Room>().Add(new Room { ResortId = Guid.CreateVersion7(), RoomNumber = "9" });
            await uow2.SaveChangesAsync();
        });
    }
}
