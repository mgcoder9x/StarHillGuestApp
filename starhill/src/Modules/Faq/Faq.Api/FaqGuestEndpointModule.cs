using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Application;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Faq.Api;

/// <summary>
/// Nửa-Api GUEST module Faq (E-Faq.4): khách đọc cây FAQ active theo ngôn ngữ. AllowAnonymous (cookie thiết bị —
/// Req 11.2). Phân giải ngữ cảnh khách qua <see cref="ICurrentGuestContextResolver"/> (GuestAccess.Contracts, C-GA.4)
/// từ cookie <see cref="GuestAccessModule.SessionCookieName"/> + <c>roomId</c> client gửi. Rule-gate (CP3) enforce
/// TRONG use case <see cref="GetGuestFaqTreeUseCase"/> (defense-in-depth — Faq là consumer đầu tiên của IRuleGate).
/// <para>
/// <b>check-before-touch</b> (QR-AD-032): touch cửa sổ SAU khi đọc thành công — KHÁC Rules-GET-no-touch. Lý do:
/// product design §resolve liệt kê <c>/faq</c> là API tương tác trượt cửa sổ; duyệt FAQ là hoạt động CHÍNH của khách
/// (không như rules-viewer đọc-lại phụ trợ); tránh <c>session_expired</c> khi đang duyệt FAQ (QR-N-045).
/// </para>
/// <c>Cache-Control: no-store</c>. Map <c>Result</c>→HTTP qua <see cref="ProblemDetailsBuilder"/>. KHÔNG log cookie.
/// </summary>
public sealed class FaqGuestEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/guest/faq", BedrockApiVersioning.V1);

        group.MapGet("/", GetFaqTreeAsync)
            .AllowAnonymous()
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("GuestGetFaqTree");
    }

    private static async Task<IResult> GetFaqTreeAsync(
        Guid roomId,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult> useCase,
        HttpContext http,
        CancellationToken ct,
        string? lang = null)
    {
        http.Response.Headers["Cache-Control"] = "no-store";

        var sessionKey = http.Request.Cookies[GuestAccessModule.SessionCookieName];
        var contextResult = await contextResolver.ResolveAsync(sessionKey, roomId, ct).ConfigureAwait(false);
        if (contextResult.IsFailure)
        {
            return Problem(contextResult.Error, http);
        }

        var context = contextResult.Value;
        var result = await useCase
            .ExecuteAsync(new GetGuestFaqTreeInput(context.ResortId, context.GuestVisitId, lang), ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Problem(result.Error, http);
        }

        // Duyệt FAQ là hành vi tương tác hợp lệ → trượt cửa sổ SAU khi thành công (check-before-touch — QR-AD-032/QR-N-045).
        await contextResolver.TouchAsync(context.GuestVisitId, ct).ConfigureAwait(false);

        return Results.Ok(result.Value);
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
