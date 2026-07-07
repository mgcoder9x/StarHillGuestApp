using ResortQr.Api.ErrorHandling;
using ResortQr.Api.Security;
using ResortQr.Application.Common;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Rooms;
using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ResortQr.Api.Endpoints;

public sealed record CreateRoomRequest(string RoomNumber, string? Building, int? Floor);

public sealed record UpdateRoomRequest(string RoomNumber, string? Building, int? Floor);

public sealed record ChangeRoomStatusRequest(string Status);

public sealed record RevokeTokenRequest(string? Reason);

/// <summary>
/// Endpoint admin quản trị phòng + QR (16 §8). Phân quyền PER-ENDPOINT: GET = RequireStaff (xem);
/// mutation + QR = RequireAdmin. (PDF nhãn hoãn — QuestPDF license TK-019.)
/// </summary>
public static class AdminRoomEndpoints
{
    private const string BasePath = "/api/admin/rooms";

    public static IEndpointRouteBuilder MapResortQrAdminRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BasePath);

        group.MapGet("/", ListAsync).RequireAuthorization(ResortQrAuthExtensions.RequireStaffPolicy);
        group.MapGet("/{id:guid}", GetByIdAsync).RequireAuthorization(ResortQrAuthExtensions.RequireStaffPolicy);
        group.MapGet("/{id:guid}/qr.png", RenderQrAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);

        group.MapPost("/", CreateAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);
        group.MapPut("/{id:guid}/status", ChangeStatusAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);
        group.MapPost("/{id:guid}/revoke-token", RevokeTokenAsync).RequireAuthorization(ResortQrAuthExtensions.RequireAdminPolicy);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        [FromServices] IRoomQueries queries,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await queries.ListAsync(new PagedRequest(page ?? 1, pageSize ?? 20), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        [FromServices] IRoomQueries queries,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var room = await queries.GetByIdAsync(id, cancellationToken);
        return room is null
            ? Result.Fail<RoomListItem>(RoomsErrors.RoomNotFound).ToHttpResult(httpContext)
            : TypedResults.Ok(room);
    }

    private static async Task<IResult> CreateAsync(
        CreateRoomRequest request,
        IUseCase<CreateRoomInput, CreateRoomResult> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new CreateRoomInput(request.RoomNumber, request.Building, request.Floor), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created($"{BasePath}/{result.Value.RoomId}", result.Value)
            : result.ToHttpResult(httpContext);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateRoomRequest request,
        ICommandUseCase<UpdateRoomInput> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new UpdateRoomInput(id, request.RoomNumber, request.Building, request.Floor), cancellationToken);
        return result.ToHttpResult(httpContext);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeRoomStatusRequest request,
        ICommandUseCase<ChangeRoomStatusInput> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RoomStatus>(request.Status, ignoreCase: true, out var status))
        {
            return Result.Fail(CommonErrors.Validation("Trạng thái phòng không hợp lệ.")).ToHttpResult(httpContext);
        }

        var result = await useCase.ExecuteAsync(new ChangeRoomStatusInput(id, status), cancellationToken);
        return result.ToHttpResult(httpContext);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ICommandUseCase<Guid> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);
        return result.ToHttpResult(httpContext);
    }

    private static async Task<IResult> RevokeTokenAsync(
        Guid id,
        RevokeTokenRequest? request,
        IUseCase<RotateRoomTokenInput, RotateRoomTokenResult> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new RotateRoomTokenInput(id, request?.Reason), cancellationToken);
        return result.ToHttpResult(httpContext);
    }

    private static async Task<IResult> RenderQrAsync(
        Guid id,
        IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new RenderRoomQrPngInput(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToHttpResult(httpContext);
        }

        // Token nhạy cảm → không cache ở proxy/browser.
        httpContext.Response.Headers.CacheControl = "no-store";
        return TypedResults.File(result.Value.Png, "image/png");
    }
}
