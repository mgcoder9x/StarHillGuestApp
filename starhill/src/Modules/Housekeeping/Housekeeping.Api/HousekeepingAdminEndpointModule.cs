using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Housekeeping.Application;
using Housekeeping.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;

namespace Housekeeping.Api;

// ---- DTO Api (client KHÔNG set ResortId/actor; server phân giải). ----
public sealed record SetHousekeepingStatusRequest(HousekeepingStatus Status);

public sealed record CompleteByRoomRequest(Guid RoomId);

public sealed record CompleteByTokenRequest(string Token);

public sealed record CreateHousekeepingRequest(Guid RoomId);

public sealed record HousekeepingTicketResponse(Guid TicketId, HousekeepingStatus Status);

/// <summary>
/// Nửa-Api ADMIN module Housekeeping (H-Hk.3): board + hoàn tất (complete-by-room/token) + set-status + tạo chủ động.
/// Tất cả <see cref="StarHillPolicies.RequireStaff"/> (Req 6/9). Actor = <see cref="ICurrentUser.UserId"/> (ghi
/// CompletedByUserId — Req 6.8). ResortId phân giải server qua <see cref="IResortSettingsQuery"/> (board + tạo). Map
/// <c>Result</c>→HTTP qua <see cref="ProblemDetailsBuilder"/>. Board đọc qua <see cref="IHousekeepingReader"/> (read port Application).
/// </summary>
public sealed class HousekeepingAdminEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/housekeeping", BedrockApiVersioning.V1);

        group.MapGet("/", BoardAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("HousekeepingBoard");
        group.MapPost("/", CreateByStaffAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("HousekeepingCreateByStaff");
        group.MapPost("/{ticketId:guid}/status", SetStatusAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("HousekeepingSetStatus");
        group.MapPost("/complete-by-room", CompleteByRoomAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("HousekeepingCompleteByRoom");
        group.MapPost("/complete-by-token", CompleteByTokenAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("HousekeepingCompleteByToken");
    }

    private static async Task<IResult> BoardAsync(
        IHousekeepingReader reader,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct,
        HousekeepingStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(HousekeepingErrors.ConfigurationUnavailable, http);
        }

        var board = await reader.ListBoardAsync(resortId.Value, status, new PagedRequest(page, pageSize), ct).ConfigureAwait(false);
        return Results.Ok(board);
    }

    private static async Task<IResult> CreateByStaffAsync(
        CreateHousekeepingRequest request,
        IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult> useCase,
        IResortSettingsQuery settingsQuery,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(HousekeepingErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase
            .ExecuteAsync(new CreateHousekeepingByStaffInput(resortId.Value, request.RoomId, currentUser.UserId), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new HousekeepingTicketResponse(result.Value.TicketId, result.Value.Status))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> SetStatusAsync(
        Guid ticketId,
        SetHousekeepingStatusRequest request,
        IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult> useCase,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new SetHousekeepingStatusInput(ticketId, request.Status, currentUser.UserId, null), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new HousekeepingTicketResponse(result.Value.TicketId, result.Value.Status))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> CompleteByRoomAsync(
        CompleteByRoomRequest request,
        IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult> useCase,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new CompleteHousekeepingByRoomInput(request.RoomId, currentUser.UserId), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new HousekeepingTicketResponse(result.Value.TicketId, result.Value.Status))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> CompleteByTokenAsync(
        CompleteByTokenRequest request,
        IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult> useCase,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new CompleteHousekeepingByTokenInput(request.Token, currentUser.UserId), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new HousekeepingTicketResponse(result.Value.TicketId, result.Value.Status))
            : Problem(result.Error, http);
    }

    private static async Task<Guid?> ResolveResortIdAsync(IResortSettingsQuery settingsQuery, CancellationToken ct)
    {
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        return settings?.ResortId;
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
