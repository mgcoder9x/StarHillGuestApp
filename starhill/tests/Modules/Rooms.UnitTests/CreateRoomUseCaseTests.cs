using System.Linq.Expressions;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Entities;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Domain;
using Xunit;

namespace Rooms.UnitTests;

/// <summary>
/// GUARD P1(a) — CreateRoom thẩm định tham chiếu resort qua <see cref="IResortExistenceQuery"/> (thay FK chéo-schema,
/// QR-DV-003). Thuần, không DB (fake port) → chạy mọi máy KHÔNG cần Docker. Kiểm:
/// (1) resort KHÔNG tồn tại → <see cref="RoomsErrors.ResortNotFound"/>, DỪNG SỚM (không sinh token, không Add, không Save);
/// (2) resort tồn tại → tạo phòng + token, Save đúng một lần.
/// </summary>
public sealed class CreateRoomUseCaseTests
{
    private static readonly Guid ResortId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Returns_resort_not_found_and_writes_nothing_when_resort_absent()
    {
        var rooms = new FakeRepository<Room>();
        var tokens = new FakeRepository<RoomQrToken>();
        var uow = new FakeUnitOfWork();
        var tokenGen = new FakeTokenGenerator();
        var sut = new CreateRoomUseCase(
            rooms, tokens, uow, new FakeCurrentUser(), new FakeClock(),
            tokenGen, new FakeResortExistenceQuery(exists: false));

        var result = await sut.ExecuteAsync(new CreateRoomInput(ResortId, "101", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(RoomsErrors.ResortNotFound, result.Error);
        // Dừng SỚM: không side-effect nào xảy ra.
        Assert.Empty(rooms.Added);
        Assert.Empty(tokens.Added);
        Assert.Equal(0, uow.SaveCount);
        Assert.Equal(0, tokenGen.CallCount);
    }

    [Fact]
    public async Task Creates_room_and_active_token_when_resort_exists()
    {
        var rooms = new FakeRepository<Room>();
        var tokens = new FakeRepository<RoomQrToken>();
        var uow = new FakeUnitOfWork();
        var sut = new CreateRoomUseCase(
            rooms, tokens, uow, new FakeCurrentUser(), new FakeClock(),
            new FakeTokenGenerator(), new FakeResortExistenceQuery(exists: true));

        var result = await sut.ExecuteAsync(new CreateRoomInput(ResortId, "101", "A", 2));

        Assert.True(result.IsSuccess);
        var room = Assert.Single(rooms.Added);
        Assert.Equal(ResortId, room.ResortId);
        Assert.Equal("101", room.RoomNumber);
        Assert.Equal(RoomStatus.Active, room.Status);
        var token = Assert.Single(tokens.Added);
        Assert.Equal(RoomQrTokenStatus.Active, token.Status);
        Assert.Equal(1, token.Version);
        Assert.Equal(room.Id, token.RoomId);
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(result.Value!.Token, token.Token);
    }

    // ---- Fakes (in-memory, không DB) ----

    private sealed class FakeResortExistenceQuery(bool exists) : IResortExistenceQuery
    {
        public Task<bool> ExistsAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(exists);
    }

    private sealed class FakeRepository<T> : IRepository<T>
        where T : Entity
    {
        public List<T> Added { get; } = [];

        public void Add(T entity) => Added.Add(entity);

        // CreateRoom qua RoomTokenFactory chỉ gọi AnyAsync(token trùng?) → luôn false (token duy nhất).
        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult(false);

        public ValueTask<T?> FindByIdAsync(Guid id, CancellationToken ct = default) => ValueTask.FromResult<T?>(null);

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
            Task.FromResult<T?>(null);

        public void Update(T entity) { }

        public void Remove(T entity) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            return Task.FromResult(2); // room + token
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default) => action(ct);
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        public int CallCount { get; private set; }

        public string NewToken(int byteLength = 32)
        {
            CallCount++;
            return "unit-test-token-value";
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }
}
