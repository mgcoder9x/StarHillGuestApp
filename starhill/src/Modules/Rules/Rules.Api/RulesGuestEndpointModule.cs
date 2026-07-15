using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rules.Application;

namespace Rules.Api;

// ---- DTO guest (không lộ session key/hash/internal — mirror GuestAccess) ----
public sealed record GuestRuleSectionResponse(
    string Key,
    int SortOrder,
    bool IsRequired,
    bool RequireScrollEnd,
    int MinReadSeconds,
    string? Title,
    string? BodyHtmlSanitized,
    string ResolvedLanguage,
    bool IsFallback,
    bool IsMissing);

public sealed record GuestRulesResponse(
    Guid PublicationId,
    int Version,
    string Language,
    IReadOnlyList<GuestRuleSectionResponse> Sections);

/// <summary>Body ack — <c>RoomId</c> (phòng khách đang xem; cookie định danh THIẾT BỊ, không phòng) + ngôn ngữ đọc.</summary>
public sealed record AcknowledgeRulesRequest(Guid RoomId, string? Lang);

public sealed record AcknowledgeRulesResponse(Guid RulePublicationId, int Version, bool AlreadyAcknowledged);

/// <summary>
/// Nửa-Api GUEST module Rules (D-Rules.4c-2): khách đọc nội quy hiện hành + xác nhận đã đọc. AllowAnonymous (cookie
/// thiết bị — Req 11.2). Phân giải ngữ cảnh khách qua <see cref="ICurrentGuestContextResolver"/> (GuestAccess.Contracts,
/// C-GA.4) từ cookie <see cref="GuestAccessModule.SessionCookieName"/> + <c>roomId</c> client gửi (thiết bị có thể ở
/// nhiều phòng). <b>check-before-touch</b> (QR-AD-032): GET rules KHÔNG touch (đọc phụ trợ); POST acknowledge touch
/// SAU khi ack thành công (hành vi nghiệp vụ hợp lệ → trượt cửa sổ). <c>Cache-Control: no-store</c>. Map <c>Result</c>→HTTP
/// qua <see cref="ProblemDetailsBuilder"/>. KHÔNG log cookie/secret. Rate-limit biên = Bedrock global IP limiter.
/// </summary>
public sealed class RulesGuestEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/guest/rules", BedrockApiVersioning.V1);

        group.MapGet("/", GetRulesAsync)
            .AllowAnonymous()
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("GuestGetRules");

        group.MapPost("/acknowledge", AcknowledgeAsync)
            .AllowAnonymous()
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("GuestAcknowledgeRules");
    }

    private static async Task<IResult> GetRulesAsync(
        Guid roomId,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<GetCurrentRulesInput, GetCurrentRulesResult> useCase,
        HttpContext http,
        CancellationToken ct,
        string? lang = null)
    {
        http.Response.Headers["Cache-Control"] = "no-store";

        var contextResult = await ResolveContextAsync(contextResolver, roomId, http, ct).ConfigureAwait(false);
        if (contextResult.IsFailure)
        {
            return Problem(contextResult.Error, http);
        }

        var context = contextResult.Value;
        var result = await useCase
            .ExecuteAsync(new GetCurrentRulesInput(context.ResortId, lang), ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Problem(result.Error, http);
        }

        // GET đọc = KHÔNG touch cửa sổ (chỉ hành vi nghiệp vụ mới touch — check-before-touch, QR-AD-032).
        var value = result.Value;
        var sections = value.Sections
            .Select(s => new GuestRuleSectionResponse(
                s.Key, s.SortOrder, s.IsRequired, s.RequireScrollEnd, s.MinReadSeconds,
                s.Title, s.BodyHtmlSanitized, s.ResolvedLanguage, s.IsFallback, s.IsMissing))
            .ToList();

        return Results.Ok(new GuestRulesResponse(value.PublicationId, value.Version, value.Language, sections));
    }

    private static async Task<IResult> AcknowledgeAsync(
        AcknowledgeRulesRequest request,
        ICurrentGuestContextResolver contextResolver,
        IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult> useCase,
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
            .ExecuteAsync(new AcknowledgeRulesInput(
                context.ResortId, context.RoomId, context.GuestSessionId, context.GuestVisitId, request.Lang), ct)
            .ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Problem(result.Error, http);
        }

        // Ack LÀ hành vi nghiệp vụ hợp lệ → trượt cửa sổ SAU khi thành công (check-before-touch — QR-AD-032).
        await contextResolver.TouchAsync(context.GuestVisitId, ct).ConfigureAwait(false);

        var value = result.Value;
        return Results.Ok(new AcknowledgeRulesResponse(value.RulePublicationId, value.Version, value.AlreadyAcknowledged));
    }

    private static async Task<Result<CurrentGuestContext>> ResolveContextAsync(
        ICurrentGuestContextResolver resolver, Guid roomId, HttpContext http, CancellationToken ct)
    {
        // Cookie thiết bị (canonical cross-module) — KHÔNG log giá trị.
        var sessionKey = http.Request.Cookies[GuestAccessModule.SessionCookieName];
        return await resolver.ResolveAsync(sessionKey, roomId, ct).ConfigureAwait(false);
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
