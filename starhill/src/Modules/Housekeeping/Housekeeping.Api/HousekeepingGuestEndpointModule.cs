using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Contracts;
using Housekeeping.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Housekeeping.Api;

/// <summary>Body tạo yêu cầu dọn phòng — <c>RoomId</c> (phòng khách đang xem; cookie định danh THIẾT BỊ, không phòng) +
/// <c>Details</c> chi tiết form (loại dịch vụ/thời gian/vật dụng/ghi chú). Server KHÔNG tin ResortId/session client — resolve từ context.</summary>
public sealed record RequestHousekeepingRequest(Guid RoomId, HousekeepingRequestDetails Details);

public sealed record RequestHousekeepingResponse(Guid TicketId, Housekeeping.Domain.HousekeepingStatus Status, bool AlreadyOpen);

/// <summary>
/// Nửa-Api GUEST module Housekeeping (H-Hk.3): khách tạo yêu cầu dọn phòng + xem trạng thái. AllowAnonymous (cookie
/// thiết bị — Req 11.2). Resolve ngữ cảnh qua <see cref="ICurrentGuestContextResolver"/> (cookie
/// <see cref="GuestAccessModule.SessionCookieName"/> + roomId). Rule-gate (CP3) + feature-flag enforce TRONG use case
/// <see cref="RequestHousekeepingUseCase"/>. <b>check-before-touch</b> (QR-AD-032): POST create touch SAU thành công
/// (hành vi nghiệp vụ); GET status KHÔNG touch (đọc phụ trợ — mirror Rules-GET, QR-N-045). <c>no-store</c>. KHÔNG log cookie.
/// </summary>
public sealed class HousekeepingGuestEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/guest/housekeeping", BedrockApiVersioning.V1);

        group.MapPost("/", RequestAsync)
            .AllowAnonymous().MapToApiVersion(BedrockApiVersioning.V1).WithName("GuestRequestHousekeeping");
        group.MapGet("/", GetStatusAsync)
            .AllowAnonymous().MapToApiVersion(BedrockApiVersioning.V1).WithName("GuestGetHousekeepingStatus");
    }

    private static async Task<IResult> RequestAsync(
        RequestHousekeepingRequest request,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<RequestHousekeepingInput, RequestHousekeepingResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        http.Response.Headers["Cache-Control"] = "no-store";

        var contextResult = await ResolveContextAsync(contextResolver, request.RoomId, http, ct).ConfigureAwait(false);
        if (contextResult.IsFailure)
        {
            return Problem(contextResult.Error, http);
        }

        var context = contextResult.Value;
        var result = await useCase
            .ExecuteAsync(
                new RequestHousekeepingInput(
                    context.ResortId, context.RoomId, context.GuestSessionId, context.GuestVisitId, request.Details),
                ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Problem(result.Error, http);
        }

        // Tạo yêu cầu LÀ hành vi nghiệp vụ hợp lệ → trượt cửa sổ SAU khi thành công (check-before-touch — QR-AD-032).
        await contextResolver.TouchAsync(context.GuestVisitId, ct).ConfigureAwait(false);

        var value = result.Value;
        return Results.Ok(new RequestHousekeepingResponse(value.TicketId, value.Status, value.AlreadyOpen));
    }

    private static async Task<IResult> GetStatusAsync(
        Guid roomId,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        http.Response.Headers["Cache-Control"] = "no-store";

        var contextResult = await ResolveContextAsync(contextResolver, roomId, http, ct).ConfigureAwait(false);
        if (contextResult.IsFailure)
        {
            return Problem(contextResult.Error, http);
        }

        var context = contextResult.Value;
        var result = await useCase
            .ExecuteAsync(new GetRoomHousekeepingStatusInput(context.RoomId), ct)
            .ConfigureAwait(false);

        // GET đọc trạng thái = KHÔNG touch (đọc phụ trợ — chỉ tạo yêu cầu mới touch).
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, http);
    }

    private static async Task<Result<CurrentGuestContext>> ResolveContextAsync(
        ICurrentGuestContextResolver resolver, Guid roomId, HttpContext http, CancellationToken ct)
    {
        var sessionKey = http.Request.Cookies[GuestAccessModule.SessionCookieName];
        return await resolver.ResolveAsync(sessionKey, roomId, ct).ConfigureAwait(false);
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
