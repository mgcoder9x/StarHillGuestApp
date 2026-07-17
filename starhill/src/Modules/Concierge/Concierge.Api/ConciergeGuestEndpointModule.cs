using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Application;
using Concierge.Domain;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Concierge.Api;

/// <summary>Body khách gửi tin — <c>RoomId</c> (phòng khách đang xem; cookie định danh THIẾT BỊ) + <c>Body</c> plain text.</summary>
public sealed record SendGuestMessageRequest(Guid RoomId, string Body);

public sealed record SendGuestMessageResponse(Guid ConversationId, Guid MessageId, ConversationStatus Status, bool Reopened);

/// <summary>
/// Nửa-Api GUEST module Concierge (K-Con.3): khách gửi tin (POST /v1/guest/messages) + đọc hội thoại của visit hiện
/// tại (GET /v1/guest/conversation — polling fallback, Req 5.9). AllowAnonymous (cookie thiết bị — Req 11.2). Resolve
/// ngữ cảnh qua <see cref="ICurrentGuestContextResolver"/> (cookie <see cref="GuestAccessModule.SessionCookieName"/> +
/// roomId). Rule-gate (CP3) + feature-flag <c>ChatEnabled</c> enforce TRONG use case <see cref="SendGuestMessageUseCase"/>.
/// <b>check-before-touch</b> (QR-AD-032): POST gửi tin touch SAU thành công (hành vi nghiệp vụ); GET đọc KHÔNG touch
/// (đọc phụ trợ — mirror Housekeeping/Rules GET, QR-N-045). <c>no-store</c>. KHÔNG log cookie. Realtime (SignalR) chỉ
/// tăng tốc — polling này LUÔN là nguồn sự thật/fallback (K-Con.4 thêm hub).
/// </summary>
public sealed class ConciergeGuestEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/guest", BedrockApiVersioning.V1);

        group.MapPost("/messages", SendAsync)
            .AllowAnonymous().MapToApiVersion(BedrockApiVersioning.V1).WithName("GuestSendConciergeMessage");
        group.MapGet("/conversation", GetConversationAsync)
            .AllowAnonymous().MapToApiVersion(BedrockApiVersioning.V1).WithName("GuestGetConciergeConversation");
    }

    private static async Task<IResult> SendAsync(
        SendGuestMessageRequest request,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<SendGuestMessageInput, SendGuestMessageResult> useCase,
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
                new SendGuestMessageInput(context.ResortId, context.RoomId, context.GuestSessionId, context.GuestVisitId, request.Body),
                ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Problem(result.Error, http);
        }

        // Gửi tin LÀ hành vi nghiệp vụ hợp lệ → trượt cửa sổ SAU khi thành công (check-before-touch — QR-AD-032).
        await contextResolver.TouchAsync(context.GuestVisitId, ct).ConfigureAwait(false);

        var value = result.Value;
        return Results.Ok(new SendGuestMessageResponse(value.ConversationId, value.MessageId, value.Status, value.Reopened));
    }

    private static async Task<IResult> GetConversationAsync(
        Guid roomId,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<GetGuestConversationInput, GetGuestConversationResult> useCase,
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
            .ExecuteAsync(new GetGuestConversationInput(context.GuestVisitId), ct)
            .ConfigureAwait(false);

        // GET đọc hội thoại = KHÔNG touch (đọc phụ trợ — chỉ gửi tin mới touch).
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
