using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rooms.Application;
using Rooms.Domain;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Rooms.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc CHỈ-Npgsql: partial unique <c>ux_room_number</c>
/// (số phòng duy nhất theo resort TRONG PHẠM VI chưa xóa mềm — filter bool Postgres-specific). SKIP nếu thiếu
/// Docker (N-067). Áp migration thật (đường production) để chứng minh index có trong migration.
/// </summary>
public sealed class RoomsPostgresConstraintTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker → skip.
        catch (Exception)
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_available)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class FixedClock : Bedrock.Application.Ports.Time.IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequentialTokenGenerator : Bedrock.Application.Ports.Security.ITokenGenerator
    {
        private int _counter;
        public string NewToken(int byteLength = 32) =>
            $"tok-{System.Threading.Interlocked.Increment(ref _counter):D4}-{Guid.NewGuid():N}";
    }

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<Bedrock.Application.Ports.Time.IClock, FixedClock>();
        services.AddSingleton<Bedrock.Application.Ports.Security.ITokenGenerator, SequentialTokenGenerator>();
        services.AddRoomsInfrastructure(o => o.UseNpgsql(_container.GetConnectionString()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static Room NewRoom(Guid resortId, string number) => new()
    {
        ResortId = resortId,
        RoomNumber = number,
        Status = RoomStatus.Active,
    };

    [SkippableFact]
    public async Task Room_number_is_unique_per_resort_among_live_rooms_but_reusable_after_soft_delete()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.MigrateAsync();
        }

        var resortId = Guid.CreateVersion7();

        // (1) Hai phòng CÙNG số + cùng resort (đều sống) → vi phạm ux_room_number.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            db.Rooms.Add(NewRoom(resortId, "A-100"));
            await db.SaveChangesAsync();

            db.Rooms.Add(NewRoom(resortId, "A-100"));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // (2) Xóa mềm phòng "A-100" rồi tạo lại cùng số → CHO PHÉP (partial filter loại hàng đã xóa).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            var existing = await db.Rooms.FirstAsync(r => r.RoomNumber == "A-100");
            db.Rooms.Remove(existing); // soft-delete
            await db.SaveChangesAsync();

            db.Rooms.Add(NewRoom(resortId, "A-100"));
            await db.SaveChangesAsync(); // không ném — số phòng tái dùng được sau soft-delete.
        }
    }

    [SkippableFact]
    public async Task CreateRoom_duplicate_number_maps_unique_violation_to_room_number_taken()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.MigrateAsync();
        }

        var resortId = Guid.CreateVersion7();

        // Phòng đầu OK.
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
            var first = await uc.ExecuteAsync(new CreateRoomInput(resortId, "A-100", null, null));
            Assert.True(first.IsSuccess);
        }

        // Phòng trùng số cùng resort → EfUnitOfWork dịch 23505 (ux_room_number) → UniqueConstraintViolationException
        // → CreateRoomUseCase ánh xạ RoomNumberTaken (validation_error). Chứng minh chuỗi dịch đầu-cuối (QR-AD-010).
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
            var dup = await uc.ExecuteAsync(new CreateRoomInput(resortId, "A-100", null, null));
            Assert.True(dup.IsFailure);
            Assert.Equal(RoomsErrors.RoomNumberTaken.Code, dup.Error.Code);
        }
    }
}
