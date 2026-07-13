using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Domain;
using StarHill.Authorization;

namespace Rooms.Api;

// ---- DTO tầng Api (tách khỏi command Application — D-E). Client KHÔNG set ResortId (server phân giải — D-D). ----
public sealed record CreateRoomRequest(string RoomNumber, string? Building, int? Floor);

/// <summary>Response tạo phòng — CHỈ lộ <see cref="TokenPreview"/> (đã mask), KHÔNG token thô (D-B/CP1).</summary>
public sealed record CreateRoomResponse(Guid RoomId, string TokenPreview);

public sealed record UpdateRoomRequest(string RoomNumber, string? Building, int? Floor);

public sealed record ChangeRoomStatusRequest(RoomStatus Status);

public sealed record RotateRoomTokenRequest(string? Reason);

/// <summary>Response rotate — chỉ preview (D-B); QR mới lấy qua <c>GET /v1/rooms/{id}/qr.png</c>.</summary>
public sealed record RotateRoomTokenResponse(string TokenPreview);

/// <summary>
/// Nửa-Api module Rooms (B-Rooms.3): endpoint admin quản lý phòng + QR. Discovery qua <see cref="IEndpointModule"/>
/// (Host <c>UseBedrockApi</c> resolve + gọi <see cref="MapEndpoints"/>). Map <c>Result</c>→HTTP qua
/// <see cref="ProblemDetailsBuilder"/> (nguồn DUY NHẤT shape lỗi — N-041). Ánh xạ role theo Req 7.6:
/// tạo/sửa/đổi-trạng-thái/xoá/rotate = <see cref="StarHillPolicies.RequireAdmin"/>; render QR (xem) =
/// <see cref="StarHillPolicies.RequireStaff"/> (Admin superset). ResortId phân giải server-side qua
/// <see cref="IResortSettingsQuery"/> (single-resort — QR-DV-004) → giữ Rooms.Application ⊥ ResortConfig.
/// </summary>
public sealed class RoomsEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Versioned qua cơ chế base (F32): route thật = /v1/rooms/... (khớp QR-DV-001).
        var group = endpoints.MapVersionedGroup("/rooms", BedrockApiVersioning.V1);

        // --- Admin-only (Req 7.6): tạo/sửa/đổi-trạng-thái/xoá phòng + sinh/thu hồi QR ---
        group.MapPost("/", CreateRoomAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsCreate");

        group.MapPut("/{roomId:guid}", UpdateRoomAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsUpdate");

        group.MapPatch("/{roomId:guid}/status", ChangeStatusAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsChangeStatus");

        group.MapDelete("/{roomId:guid}", DeleteRoomAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsDelete");

        group.MapPost("/{roomId:guid}/rotate-token", RotateTokenAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsRotateToken");

        // --- Staff+Admin (Req 7.6 "Staff xem"): render QR PNG của token Active (không sinh/rotate) ---
        group.MapGet("/{roomId:guid}/qr.png", RenderQrPngAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RoomsRenderQrPng");
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomRequest request,
        IUseCase<CreateRoomInput, CreateRoomResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // D-D: single-resort → resortId lấy từ settings snapshot. Chưa seed → cấu hình chưa hợp lệ.
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            return Problem(RoomsErrors.InvalidConfiguration, http);
        }

        var input = new CreateRoomInput(settings.ResortId, request.RoomNumber, request.Building, request.Floor);
        var result = await useCase.ExecuteAsync(input, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/rooms/{result.Value.RoomId}", new CreateRoomResponse(result.Value.RoomId, result.Value.TokenPreview))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> UpdateRoomAsync(
        Guid roomId,
        UpdateRoomRequest request,
        ICommandUseCase<UpdateRoomInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new UpdateRoomInput(roomId, request.RoomNumber, request.Building, request.Floor), ct)
            .ConfigureAwait(false);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid roomId,
        ChangeRoomStatusRequest request,
        ICommandUseCase<ChangeRoomStatusInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new ChangeRoomStatusInput(roomId, request.Status), ct)
            .ConfigureAwait(false);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> DeleteRoomAsync(
        Guid roomId,
        ICommandUseCase<Guid> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(roomId, ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> RotateTokenAsync(
        Guid roomId,
        RotateRoomTokenRequest request,
        IUseCase<RotateRoomTokenInput, RotateRoomTokenResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new RotateRoomTokenInput(roomId, request.Reason), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(new RotateRoomTokenResponse(result.Value.TokenPreview))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> RenderQrPngAsync(
        Guid roomId,
        IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new RenderRoomQrPngInput(roomId), ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.File(result.Value.Png, "image/png")
            : Problem(result.Error, http);
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
