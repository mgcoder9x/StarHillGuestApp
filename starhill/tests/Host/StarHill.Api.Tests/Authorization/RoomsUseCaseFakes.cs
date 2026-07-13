using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using ResortConfig.Application;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Domain;

namespace StarHill.Api.Tests.Authorization;

// Fake use case + query cho guard auth Rooms: trả success KHÔNG chạm DB → cô lập test ở tầng routing/authorization
// (đường Admin-OK vẫn cần handler chạy tới cùng; fake giúp verify KHÔNG cần Postgres/Docker).

internal sealed class FakeCreateRoom : IUseCase<CreateRoomInput, CreateRoomResult>
{
    public static readonly Guid RoomId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<Result<CreateRoomResult>> ExecuteAsync(CreateRoomInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new CreateRoomResult(RoomId, "raw-secret-token", "raw-***")));
}

internal sealed class FakeRotateToken : IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>
{
    public Task<Result<RotateRoomTokenResult>> ExecuteAsync(RotateRoomTokenInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new RotateRoomTokenResult("raw-secret-token", "raw-***")));
}

internal sealed class FakeRenderQrPng : IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47]; // "‰PNG" magic (đủ để assert content).

    public Task<Result<RenderRoomQrPngResult>> ExecuteAsync(RenderRoomQrPngInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new RenderRoomQrPngResult(Png)));
}

internal sealed class FakeUpdateRoom : ICommandUseCase<UpdateRoomInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(UpdateRoomInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeChangeStatus : ICommandUseCase<ChangeRoomStatusInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(ChangeRoomStatusInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeDeleteRoom : ICommandUseCase<Guid>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(Guid input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeRoomQueries : IRoomQueries
{
    public static readonly Guid KnownRoomId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static RoomListItem Item(Guid id) =>
        new(id, "A-101", "A", 1, RoomStatus.Active, "abc12…", 1, DateTimeOffset.UnixEpoch);

    public Task<PagedResult<RoomListItem>> ListAsync(RoomStatus? status, PagedRequest paging, CancellationToken ct = default) =>
        Task.FromResult(new PagedResult<RoomListItem>(new List<RoomListItem> { Item(KnownRoomId) }, paging.SafePage, paging.SafePageSize, 1));

    public Task<RoomListItem?> GetByIdAsync(Guid roomId, CancellationToken ct = default) =>
        Task.FromResult<RoomListItem?>(roomId == KnownRoomId ? Item(roomId) : null);
}

internal sealed class FakeUpdateResortSettings : ICommandUseCase<UpdateResortSettingsInput>
{
    public string? PersistenceKey => null;

    public Task<Result> ExecuteAsync(UpdateResortSettingsInput input, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());
}

internal sealed class FakeResortSettingsQuery : IResortSettingsQuery
{
    public static readonly Guid ResortId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) =>
        Task.FromResult<ResortSettingsSnapshot?>(new ResortSettingsSnapshot(
            ResortId: ResortId,
            FaqEnabled: true,
            ChatEnabled: true,
            HousekeepingEnabled: true,
            RequireRuleAckForFaq: false,
            RequireRuleAckForChat: false,
            RequireRuleAckForHousekeeping: false,
            PortalWindowMinutes: 30,
            VisitIdleExpiryHours: 24,
            GuestWebBaseUrl: "https://guest.example.com",
            MaxMessageLength: 1000,
            MessageRateLimitPerMinute: 10,
            HousekeepingRateLimitPerHour: 5));
}
