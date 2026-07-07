using ResortQr.Api.ErrorHandling;
using ResortQr.Application.Abstractions;
using ResortQr.Application.Common;
using ResortQr.Application.Identity;
using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace ResortQr.Api.Endpoints;

public sealed record LoginRequest(string Email, string Password);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Endpoint auth nền: login/refresh/logout + /me. Access token trả body (client giữ in-memory);
/// refresh token đặt cookie HttpOnly/Secure/SameSite=Strict, path giới hạn /auth (không lộ ra JS, không gửi cross-site).
/// </summary>
public static class AuthEndpoints
{
    private const string RefreshCookieName = "refresh_token";
    private const string BasePath = "/auth";

    public static IEndpointRouteBuilder MapResortQrAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(BasePath).RequireRateLimiting(RateLimiting.ResortQrRateLimitExtensions.AuthPolicy);
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).AllowAnonymous();
        group.MapGet("/me", GetMe).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IUseCase<LoginCommand, AuthTokens> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToHttpResult(httpContext);
        }

        AppendRefreshCookie(httpContext, result.Value);
        var access = result.Value.AccessToken;
        return TypedResults.Ok(new TokenResponse(access.Token, access.ExpiresAt));
    }

    private static async Task<IResult> RefreshAsync(
        IUseCase<RefreshCommand, AuthTokens> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var presented = httpContext.Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(presented))
        {
            return Result.Fail<TokenResponse>(AuthErrors.InvalidRefreshToken).ToHttpResult(httpContext);
        }

        var result = await useCase.ExecuteAsync(new RefreshCommand(presented), cancellationToken);
        if (result.IsFailure)
        {
            DeleteRefreshCookie(httpContext);
            return result.ToHttpResult(httpContext);
        }

        AppendRefreshCookie(httpContext, result.Value);
        var access = result.Value.AccessToken;
        return TypedResults.Ok(new TokenResponse(access.Token, access.ExpiresAt));
    }

    private static async Task<IResult> LogoutAsync(
        ICommandUseCase<LogoutCommand> useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var presented = httpContext.Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(presented))
        {
            await useCase.ExecuteAsync(new LogoutCommand(presented), cancellationToken);
        }

        DeleteRefreshCookie(httpContext);
        return TypedResults.NoContent();
    }

    private static Ok<MeResponse> GetMe(ICurrentUser currentUser) =>
        TypedResults.Ok(new MeResponse(currentUser.UserId, currentUser.Roles));

    private static void AppendRefreshCookie(HttpContext httpContext, AuthTokens tokens)
    {
        httpContext.Response.Cookies.Append(RefreshCookieName, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = BasePath,
            Expires = tokens.RefreshTokenExpiresAt,
            IsEssential = true,
        });
    }

    private static void DeleteRefreshCookie(HttpContext httpContext) =>
        httpContext.Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = BasePath });

    private sealed record MeResponse(Guid? UserId, IReadOnlyCollection<string> Roles);
}
