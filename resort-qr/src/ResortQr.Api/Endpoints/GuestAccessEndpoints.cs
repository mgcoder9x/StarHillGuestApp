using ResortQr.Api.ErrorHandling;
using ResortQr.Api.GuestAccess;
using ResortQr.Application.Common;
using ResortQr.Application.GuestAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ResortQr.Api.Endpoints;

/// <summary>Body resolve (KHÔNG chứa session key raw — key đó chỉ đặt vào cookie).</summary>
public sealed record ResolveResponse(
    Guid RoomId,
    string RoomNumber,
    Guid ResortId,
    string ResortName,
    Guid VisitId,
    DateTimeOffset PortalWindowExpiresAt,
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    string? DefaultLanguageCode);

/// <summary>
/// Endpoint guest: `GET /api/guest/resolve/{token}` (AllowAnonymous). Đọc cookie thiết bị → use case →
/// set cookie nếu phát session mới (HttpOnly/Secure/SameSite=Lax/Path=/) → 200 JSON hoặc ProblemDetails.
/// Rate-limit hoãn (increment sau). SameSite=Lax vì QR mở tab mới (13/03 §4).
/// </summary>
public static class GuestAccessEndpoints
{
    private const string BasePath = "/api/guest";

    public static IEndpointRouteBuilder MapResortQrGuestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BasePath);
        group.MapGet("/resolve/{token}", ResolveAsync).AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> ResolveAsync(
        string token,
        IUseCase<ResolveTokenInput, ResolveTokenResult> useCase,
        IOptions<GuestOptions> guestOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var options = guestOptions.Value;
        var currentSessionKey = httpContext.Request.Cookies[options.CookieName];

        var result = await useCase.ExecuteAsync(new ResolveTokenInput(token, currentSessionKey), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToHttpResult(httpContext);
        }

        var value = result.Value;
        if (value.IssuedSessionKey is not null)
        {
            httpContext.Response.Cookies.Append(options.CookieName, value.IssuedSessionKey, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(options.SessionCookieDays),
                IsEssential = true,
            });
        }

        return TypedResults.Ok(new ResolveResponse(
            value.RoomId,
            value.RoomNumber,
            value.ResortId,
            value.ResortName,
            value.VisitId,
            value.PortalWindowExpiresAt,
            value.FaqEnabled,
            value.ChatEnabled,
            value.HousekeepingEnabled,
            value.DefaultLanguageCode));
    }
}
