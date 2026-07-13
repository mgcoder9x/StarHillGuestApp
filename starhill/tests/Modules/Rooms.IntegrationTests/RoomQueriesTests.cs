using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rooms.Application;
using Rooms.Domain;
using Rooms.Infrastructure.DependencyInjection;
using Rooms.Infrastructure.Persistence;
using Xunit;

namespace Rooms.IntegrationTests;

/// <summary>
/// Kiểm <see cref="IRoomQueries"/> (B-Rooms.4) trên SQLite in-memory (quan hệ thật, CỤC BỘ — không Docker):
/// list kèm ActiveTokenPreview/Version; total; phòng xóa-mềm bị loại; lọc status; phân trang; GetById có/null.
/// Seed phòng qua <c>CreateRoomUseCase</c> (mỗi phòng có 1 token Active) → sát luồng thật.
/// </summary>
public sealed class RoomQueriesTests
{
    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequentialTokenGenerator : ITokenGenerator
    {
        private int _counter;
        public string NewToken(int byteLength = 32) => $"tok-{Interlocked.Increment(ref _counter):D4}-{Guid.NewGuid():N}";
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITokenGenerator, SequentialTokenGenerator>();
        services.AddSingleton<ResortConfig.Contracts.Queries.IResortExistenceQuery>(new TestResortExistenceQuery());
        services.AddRoomsInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoomsDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateRoomAsync(ServiceProvider provider, string number, string building, int floor)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRoomInput, CreateRoomResult>>();
        var result = await uc.ExecuteAsync(new CreateRoomInput(Guid.CreateVersion7(), number, building, floor));
        Assert.True(result.IsSuccess);
        return result.Value.RoomId;
    }

    [Fact]
    public async Task List_returns_items_with_active_token_preview_and_excludes_soft_deleted()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await CreateRoomAsync(provider, "A-101", "A", 1);
        await CreateRoomAsync(provider, "A-102", "A", 1);
        var deletedId = await CreateRoomAsync(provider, "B-201", "B", 2);

        await using (var scope = provider.CreateAsyncScope())
        {
            var del = scope.ServiceProvider.GetRequiredService<ICommandUseCase<Guid>>();
            Assert.True((await del.ExecuteAsync(deletedId)).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var queries = scope.ServiceProvider.GetRequiredService<IRoomQueries>();
            var page = await queries.ListAsync(status: null, new PagedRequest(1, 20));

            Assert.Equal(2, page.Total); // B-201 soft-deleted bị loại.
            Assert.Equal(2, page.Items.Count);
            Assert.Collection(page.Items,
                first => Assert.Equal("A-101", first.RoomNumber),  // ordered Building→RoomNumber.
                second => Assert.Equal("A-102", second.RoomNumber));
            Assert.All(page.Items, i =>
            {
                Assert.False(string.IsNullOrEmpty(i.ActiveTokenPreview)); // preview token Active có mặt.
                Assert.Equal(1, i.ActiveTokenVersion);
                Assert.Equal(RoomStatus.Active, i.Status);
            });
        }
    }

    [Fact]
    public async Task List_filters_by_status_and_paginates()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var r1 = await CreateRoomAsync(provider, "A-101", "A", 1);
        await CreateRoomAsync(provider, "A-102", "A", 1);
        await CreateRoomAsync(provider, "A-103", "A", 1);

        await using (var scope = provider.CreateAsyncScope())
        {
            var status = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ChangeRoomStatusInput>>();
            Assert.True((await status.ExecuteAsync(new ChangeRoomStatusInput(r1, RoomStatus.Maintenance))).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var queries = scope.ServiceProvider.GetRequiredService<IRoomQueries>();

            var maintenance = await queries.ListAsync(RoomStatus.Maintenance, new PagedRequest(1, 20));
            Assert.Equal(1, maintenance.Total);
            Assert.Equal("A-101", Assert.Single(maintenance.Items).RoomNumber);

            var pageOne = await queries.ListAsync(status: null, new PagedRequest(1, 2));
            Assert.Equal(3, pageOne.Total);
            Assert.Equal(2, pageOne.Items.Count);

            var pageTwo = await queries.ListAsync(status: null, new PagedRequest(2, 2));
            Assert.Equal(3, pageTwo.Total);
            Assert.Equal("A-103", Assert.Single(pageTwo.Items).RoomNumber);
        }
    }

    [Fact]
    public async Task GetById_returns_room_or_null()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var id = await CreateRoomAsync(provider, "A-101", "A", 1);

        await using var scope = provider.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IRoomQueries>();

        var found = await queries.GetByIdAsync(id);
        Assert.NotNull(found);
        Assert.Equal("A-101", found!.RoomNumber);
        Assert.False(string.IsNullOrEmpty(found.ActiveTokenPreview));

        Assert.Null(await queries.GetByIdAsync(Guid.CreateVersion7()));
    }
}
