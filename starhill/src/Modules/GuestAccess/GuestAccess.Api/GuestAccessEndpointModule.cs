using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace GuestAccess.Api;

/// <summary>Body resolve — token QR trong BODY (KHÔNG path/query → không lọt access-log; QR-DV-006).</summary>
public sealed record ResolveRequest(string Token);

/// <summary>
/// Response resolve. KHÔNG chứa session key/hash/QR token/ExpiresAt idle/nội bộ (QR-AD-025) — raw session key CHỈ
/// đặt vào cookie <c>Set-Cookie</c>, không vào JSON.
/// </summary>
public sealed record ResolveRoomResponse(Guid Id, string Number, string? Building, int? Floor);
public sealed record ResolveResortResponse(Guid Id, string Name, string? LogoUrl);
public sealed record ResolveVisitResponse(Guid Id, DateTimeOffset PortalWindowExpiresAt);
public sealed record ResolveFeaturesResponse(
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RuleAckRequiredForFaq,
    bool RuleAckRequiredForChat,
    bool RuleAckRequiredForHousekeeping);

public sealed record ResolveResponse(
    ResolveRoomResponse Room,
    ResolveResortResponse Resort,
    IReadOnlyList<string> Languages,
    string DefaultLanguage,
    ResolveVisitResponse Visit,
    ResolveFeaturesResponse Features);

/// <summary>
/// Nửa-Api module GuestAccess (C-GA.3): endpoint guest công khai <c>POST /v1/guest/resolve</c> (AllowAnonymous —
/// Req 11.2). Token trong body (QR-DV-006). Đặt cookie thiết bị <c>__Host-</c> (HttpOnly/Secure/SameSite=Lax/Path=/)
/// CHỈ khi phát session mới. <c>Cache-Control: no-store</c> (không cache response chứa ngữ cảnh visit). Map
/// <c>Result</c>→HTTP qua <see cref="ProblemDetailsBuilder"/> (nguồn shape lỗi DUY NHẤT). Rate-limit biên do
/// Bedrock.Api global limiter (F16, partition IP) áp sẵn ở <c>UseBedrockApi</c> #9. KHÔNG log token/cookie (Req 11.6).
/// </summary>
public sealed class GuestAccessEndpointModule : IEndpointModule
{
    internal const long MaxResolveRequestBodyBytes = 1024;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/guest", BedrockApiVersioning.V1);

        group.MapPost("/resolve", ResolveAsync)
            .AllowAnonymous()
            .WithMetadata(new RequestSizeLimitAttribute(MaxResolveRequestBodyBytes))
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("GuestResolve");
    }

    private static async Task<IResult> ResolveAsync(
        ResolveRequest request,
        IUseCase<ResolveTokenInput, ResolveTokenResult> useCase,
        IOptions<GuestAccessOptions> options,
        IClock clock,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Response mang ngữ cảnh visit → không cache (proxy/browser). Đặt trước mọi nhánh return.
        http.Response.Headers["Cache-Control"] = "no-store";

        var cookieName = options.Value.CookieName;
        var currentSessionKey = http.Request.Cookies[cookieName];

        var result = await useCase
            .ExecuteAsync(new ResolveTokenInput(request.Token, currentSessionKey), ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            // KHÔNG set cookie khi lỗi. Shape lỗi chuẩn ProblemDetails (không lộ chi tiết phòng khác — CP1).
            return Results.Problem(ProblemDetailsBuilder.Build(result.Error, CorrelationContext.Resolve(http)));
        }

        var value = result.Value;

        // CHỈ set cookie khi phát session MỚI. Raw key vào cookie (không vào JSON). __Host- ⇒ Secure + Path=/ + no Domain.
        if (value.IssuedSessionKey is not null)
        {
            http.Response.Cookies.Append(cookieName, value.IssuedSessionKey, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = clock.UtcNow.AddDays(options.Value.SessionCookieDays),
                IsEssential = true,
            });
        }

        return Results.Ok(new ResolveResponse(
            new ResolveRoomResponse(value.RoomId, value.RoomNumber, value.Building, value.Floor),
            new ResolveResortResponse(value.ResortId, value.ResortName, value.LogoUrl),
            value.Languages,
            value.DefaultLanguageCode,
            new ResolveVisitResponse(value.VisitId, value.PortalWindowExpiresAt),
            new ResolveFeaturesResponse(
                value.FaqEnabled,
                value.ChatEnabled,
                value.HousekeepingEnabled,
                value.RequireRuleAckForFaq,
                value.RequireRuleAckForChat,
                value.RequireRuleAckForHousekeeping)));
    }
}
