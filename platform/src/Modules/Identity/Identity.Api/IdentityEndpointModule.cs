using Asp.Versioning.Builder;
using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Identity.Application.RefreshToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Api;

/// <summary>Request/response HTTP của endpoint refresh (DTO tầng Api — tách khỏi command Application).</summary>
public sealed record RefreshRequest(string RefreshToken);

public sealed record RefreshResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

/// <summary>
/// Nửa-Api của module Identity: khai endpoint qua <see cref="IEndpointModule"/> (discovery §6 — Host resolve
/// <c>IEnumerable&lt;IEndpointModule&gt;</c> và gọi <see cref="MapEndpoints"/>). Map <c>Result</c> → HTTP dùng
/// <see cref="ProblemDetailsBuilder"/> (nguồn DUY NHẤT shape lỗi — N-041). Endpoint refresh CÔNG KHAI (AllowAnonymous):
/// chính nó là cơ chế đổi refresh-token lấy access-token, không yêu cầu access-token hợp lệ trước.
/// </summary>
public sealed class IdentityEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Versioned qua cơ chế base (F32): route thật = /v1/identity/token/refresh.
        var group = endpoints.MapVersionedGroup("/identity", BedrockApiVersioning.V1);

        group.MapPost("/token/refresh", RefreshAsync)
            .AllowAnonymous()
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("IdentityRefreshToken");
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IUseCase<RefreshTokenCommand, RefreshTokenResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new RefreshTokenCommand(request.RefreshToken), ct)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var value = result.Value;
            return Results.Ok(new RefreshResponse(value.AccessToken, value.RefreshToken, value.RefreshTokenExpiresAt));
        }

        return Results.Problem(ProblemDetailsBuilder.Build(result.Error, CorrelationContext.Resolve(http)));
    }
}
