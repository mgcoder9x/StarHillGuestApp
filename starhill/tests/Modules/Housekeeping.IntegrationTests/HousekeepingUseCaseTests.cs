using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Housekeeping.Application;
using Housekeeping.Domain;
using Housekeeping.Infrastructure.DependencyInjection;
using Housekeeping.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;
using Xunit;

namespace Housekeeping.IntegrationTests;

/// <summary>
/// H-Hk.2 — use case Housekeeping (SQLite + fake config/gate/token-resolver). Phủ: guest request (config-null/disabled/
/// gate-fail/happy/idempotent), máy trạng thái + event-log, complete-by-room/token, create-by-staff, cancel-for-visit,
/// guest status read. Logic Application provider-agnostic → SQLite chạy cục bộ; concurrency (xmin) đo riêng Postgres.
/// </summary>
public sealed class HousekeepingUseCaseTests
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
        public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubGuestConfigQuery : IResortGuestConfigQuery
    {
        public ResortGuestConfig? Config { get; set; }
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(Config);
    }

    private sealed class FakeRuleGate : Rules.Contracts.IRuleGate
    {
        public Result Next { get; set; } = Result.Success();
        public Task<Result> EnsureAcknowledgedAsync(Guid resortId, Guid guestVisitId, Rules.Contracts.GuestFeature feature, CancellationToken ct = default) =>
            Task.FromResult(Next);
    }

    private sealed class FakeRoomTokenResolver : IRoomTokenResolver
    {
        public RoomResolution? Next { get; set; }
        public Task<RoomResolution?> ResolveActiveTokenAsync(string token, CancellationToken ct = default) => Task.FromResult(Next);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId, bool housekeepingEnabled) => new(
        resortId, "Star Hill", null, ["en"], "en", true, true, housekeepingEnabled, false, false, false, 30, 24);

    private sealed record Harness(
        ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config, FakeRuleGate Gate, FakeRoomTokenResolver Resolver);

    private static async Task<Harness> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var gate = new FakeRuleGate();
        var resolver = new FakeRoomTokenResolver();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddSingleton<Rules.Contracts.IRuleGate>(gate);
        services.AddSingleton<IRoomTokenResolver>(resolver);
        services.AddHousekeepingInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new Harness(provider, connection, config, gate, resolver);
    }

    private static async Task<Result<RequestHousekeepingResult>> RequestAsync(
        ServiceProvider provider, Guid resortId, Guid roomId, Guid visitId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>>();
        return await uc.ExecuteAsync(new RequestHousekeepingInput(resortId, roomId, Guid.CreateVersion7(), visitId));
    }

    [Fact]
    public async Task Request_missing_config_returns_configuration_unavailable()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        h.Config.Config = null;

        var result = await RequestAsync(h.Provider, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Request_disabled_returns_housekeeping_disabled()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: false);

        var result = await RequestAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7());
        Assert.False(result.IsSuccess);
        Assert.Equal("housekeeping_disabled", result.Error.Code);
    }

    [Fact]
    public async Task Request_gate_failure_returns_rule_ack_required()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        h.Gate.Next = Result.Failure(Error.Forbidden("rule_ack_required", "chưa ack"));

        var result = await RequestAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7());
        Assert.False(result.IsSuccess);
        Assert.Equal("rule_ack_required", result.Error.Code);
    }

    [Fact]
    public async Task Request_creates_ticket_and_is_idempotent()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);

        var first = await RequestAsync(h.Provider, resortId, roomId, visitId);
        Assert.True(first.IsSuccess);
        Assert.False(first.Value.AlreadyOpen);
        Assert.Equal(HousekeepingStatus.Requested, first.Value.Status);

        var second = await RequestAsync(h.Provider, resortId, roomId, visitId);
        Assert.True(second.IsSuccess);
        Assert.True(second.Value.AlreadyOpen); // idempotent — không tạo trùng.
        Assert.Equal(first.Value.TicketId, second.Value.TicketId);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
        Assert.Equal(1, await db.HousekeepingTickets.CountAsync(t => t.RoomId == roomId));
        Assert.Equal(1, await db.HousekeepingEvents.CountAsync()); // 1 event Requested.
    }

    [Fact]
    public async Task Status_machine_requested_to_inprogress_to_done_logs_each_event()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        var ticketId = (await RequestAsync(h.Provider, resortId, roomId, Guid.CreateVersion7())).Value.TicketId;

        async Task<Result<HousekeepingTicketResult>> SetStatus(HousekeepingStatus s)
        {
            await using var scope = h.Provider.CreateAsyncScope();
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>>();
            return await uc.ExecuteAsync(new SetHousekeepingStatusInput(ticketId, s, Guid.CreateVersion7(), null));
        }

        Assert.True((await SetStatus(HousekeepingStatus.InProgress)).IsSuccess);
        Assert.True((await SetStatus(HousekeepingStatus.Done)).IsSuccess);

        // Done → InProgress: chuyển không hợp lệ (terminal).
        var invalid = await SetStatus(HousekeepingStatus.InProgress);
        Assert.False(invalid.IsSuccess);
        Assert.Equal("housekeeping_invalid_transition", invalid.Error.Code);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
        var ticket = await db.HousekeepingTickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(HousekeepingStatus.Done, ticket.Status);
        Assert.NotNull(ticket.StartedAt);
        Assert.NotNull(ticket.CompletedAt);
        Assert.Equal(HousekeepingCompletionMethod.App, ticket.CompletionMethod);
        Assert.Equal(3, await db.HousekeepingEvents.CountAsync(e => e.HousekeepingTicketId == ticketId)); // Requested+InProgress+Done
    }

    [Fact]
    public async Task Set_status_ticket_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>>();
        var result = await uc.ExecuteAsync(new SetHousekeepingStatusInput(Guid.CreateVersion7(), HousekeepingStatus.Done, null, null));
        Assert.False(result.IsSuccess);
        Assert.Equal("housekeeping_ticket_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Complete_by_room_sets_done_with_method_app()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        await RequestAsync(h.Provider, resortId, roomId, Guid.CreateVersion7());

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>>();
            var result = await uc.ExecuteAsync(new CompleteHousekeepingByRoomInput(roomId, Guid.CreateVersion7()));
            Assert.True(result.IsSuccess);
            Assert.Equal(HousekeepingStatus.Done, result.Value.Status);
        }

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            var t = await db.HousekeepingTickets.SingleAsync(x => x.RoomId == roomId);
            Assert.Equal(HousekeepingCompletionMethod.App, t.CompletionMethod);
        }
    }

    [Fact]
    public async Task Complete_by_room_no_open_ticket()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>>();
        var result = await uc.ExecuteAsync(new CompleteHousekeepingByRoomInput(Guid.CreateVersion7(), null));
        Assert.False(result.IsSuccess);
        Assert.Equal("housekeeping_no_open_ticket", result.Error.Code);
    }

    [Fact]
    public async Task Complete_by_token_resolves_room_and_sets_staffscan()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        await RequestAsync(h.Provider, resortId, roomId, Guid.CreateVersion7());
        h.Resolver.Next = new RoomResolution(roomId, resortId, "A-101", null, null, IsRoomActive: true);

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>>();
            var result = await uc.ExecuteAsync(new CompleteHousekeepingByTokenInput("tok-123", Guid.CreateVersion7()));
            Assert.True(result.IsSuccess);
            Assert.Equal(HousekeepingStatus.Done, result.Value.Status);
        }

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            var t = await db.HousekeepingTickets.SingleAsync(x => x.RoomId == roomId);
            Assert.Equal(HousekeepingCompletionMethod.StaffScan, t.CompletionMethod);
        }
    }

    [Fact]
    public async Task Complete_by_token_invalid_token_returns_qr_invalid()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        h.Resolver.Next = null; // token không phân giải.

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>>();
        var result = await uc.ExecuteAsync(new CompleteHousekeepingByTokenInput("bad", null));
        Assert.False(result.IsSuccess);
        Assert.Equal("qr_invalid", result.Error.Code);
    }

    [Fact]
    public async Task Cancel_open_tickets_for_visit_cancels_and_logs()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        var ticketId = (await RequestAsync(h.Provider, resortId, Guid.CreateVersion7(), visitId)).Value.TicketId;

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CancelOpenTicketsForVisitInput, CancelOpenTicketsForVisitResult>>();
            var result = await uc.ExecuteAsync(new CancelOpenTicketsForVisitInput(visitId));
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.CancelledCount);
        }

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
            Assert.Equal(HousekeepingStatus.Cancelled, (await db.HousekeepingTickets.SingleAsync(t => t.Id == ticketId)).Status);
            Assert.Equal(2, await db.HousekeepingEvents.CountAsync(e => e.HousekeepingTicketId == ticketId)); // Requested + Cancelled
        }
    }

    [Fact]
    public async Task Get_room_status_returns_latest_ticket()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, housekeepingEnabled: true);
        await RequestAsync(h.Provider, resortId, roomId, Guid.CreateVersion7());

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult>>();
        var result = await uc.ExecuteAsync(new GetRoomHousekeepingStatusInput(roomId));
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Ticket);
        Assert.Equal(HousekeepingStatus.Requested, result.Value.Ticket!.Status);
    }

    [Fact]
    public async Task Create_by_staff_creates_and_is_idempotent()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        async Task<Result<RequestHousekeepingResult>> StaffCreate()
        {
            await using var scope = h.Provider.CreateAsyncScope();
            var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult>>();
            return await uc.ExecuteAsync(new CreateHousekeepingByStaffInput(resortId, roomId, Guid.CreateVersion7()));
        }

        var first = await StaffCreate();
        Assert.True(first.IsSuccess);
        Assert.False(first.Value.AlreadyOpen);
        var second = await StaffCreate();
        Assert.True(second.Value.AlreadyOpen); // idempotent 1-mở/phòng.
        Assert.Equal(first.Value.TicketId, second.Value.TicketId);
    }
}
