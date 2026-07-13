using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Application;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;

namespace ResortConfig.Api;

/// <summary>Body PUT settings (DTO Api — tách khỏi command Application; KHÔNG cho set ResortId).</summary>
public sealed record UpdateResortSettingsRequest(
    bool FaqEnabled,
    bool ChatEnabled,
    bool HousekeepingEnabled,
    bool RequireRuleAckForFaq,
    bool RequireRuleAckForChat,
    bool RequireRuleAckForHousekeeping,
    int PortalWindowMinutes,
    int VisitIdleExpiryHours,
    string? GuestWebBaseUrl,
    int MaxMessageLength,
    int MessageRateLimitPerMinute,
    int HousekeepingRateLimitPerHour);

/// <summary>
/// Nửa-Api module ResortConfig (B-Config.3): admin xem + sửa cấu hình resort. Discovery qua <see cref="IEndpointModule"/>.
/// Cả hai endpoint <see cref="StarHillPolicies.RequireAdmin"/> (Req 9.3 — settings là cấu hình admin). GET dùng
/// <see cref="IResortSettingsQuery"/> (read sẵn có); PUT qua <c>ICommandUseCase</c> (pipeline validation + transaction).
/// Map <c>Result</c>→HTTP qua <see cref="ProblemDetailsBuilder"/> (nguồn DUY NHẤT shape lỗi — N-041).
/// </summary>
public sealed class ResortConfigEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Route thật = /v1/resort/settings.
        var group = endpoints.MapVersionedGroup("/resort", BedrockApiVersioning.V1);

        group.MapGet("/settings", GetSettingsAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("ResortConfigGetSettings");

        group.MapPut("/settings", UpdateSettingsAsync)
            .RequireAuthorization(StarHillPolicies.RequireAdmin)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("ResortConfigUpdateSettings");
    }

    private static async Task<IResult> GetSettingsAsync(
        IResortSettingsQuery query,
        HttpContext http,
        CancellationToken ct)
    {
        var settings = await query.GetAsync(ct).ConfigureAwait(false);
        return settings is null
            ? Results.Problem(ProblemDetailsBuilder.Build(
                CommonErrors.NotFoundGeneric("Cấu hình resort chưa được khởi tạo."), CorrelationContext.Resolve(http)))
            : Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSettingsAsync(
        UpdateResortSettingsRequest request,
        ICommandUseCase<UpdateResortSettingsInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = new UpdateResortSettingsInput(
            request.FaqEnabled,
            request.ChatEnabled,
            request.HousekeepingEnabled,
            request.RequireRuleAckForFaq,
            request.RequireRuleAckForChat,
            request.RequireRuleAckForHousekeeping,
            request.PortalWindowMinutes,
            request.VisitIdleExpiryHours,
            request.GuestWebBaseUrl,
            request.MaxMessageLength,
            request.MessageRateLimitPerMinute,
            request.HousekeepingRateLimitPerHour);

        var result = await useCase.ExecuteAsync(input, ct).ConfigureAwait(false);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(ProblemDetailsBuilder.Build(result.Error, CorrelationContext.Resolve(http)));
    }
}
