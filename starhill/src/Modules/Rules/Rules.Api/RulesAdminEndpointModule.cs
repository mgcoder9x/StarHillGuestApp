using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using Rules.Application;
using StarHill.Authorization;

namespace Rules.Api;

// ---- DTO Api (tách khỏi command Application — client KHÔNG set ResortId; server phân giải single-resort). ----
public sealed record CreateRuleSectionRequest(string Key, int SortOrder, bool IsRequired, bool RequireScrollEnd, int MinReadSeconds);

public sealed record CreateRuleSectionResponse(Guid SectionId);

public sealed record UpdateRuleSectionRequest(int SortOrder, bool IsRequired, bool RequireScrollEnd, int MinReadSeconds, uint ExpectedRowVersion);

/// <summary>Body upsert bản dịch — <c>Title</c>/<c>BodyHtml</c> THÔ; use case sanitize trước khi lưu (CP12).</summary>
public sealed record UpsertRuleSectionTranslationRequest(string? Title, string? BodyHtml, uint? ExpectedRowVersion);

public sealed record UpsertRuleSectionTranslationResponse(Guid TranslationId);

public sealed record PublishRulesRequest(string? ChangeNote);

public sealed record PublishRulesResponse(Guid PublicationId, int Version);

/// <summary>
/// Nửa-Api ADMIN module Rules (D-Rules.4c-1): soạn Draft (section CRUD + bản dịch upsert sanitize-on-save) + Publish
/// snapshot. Tất cả <see cref="StarHillPolicies.RequireStaff"/> (Req 8/11.3 — nội quy thuộc quyền Staff; Admin superset).
/// ResortId phân giải SERVER-SIDE qua <see cref="IResortSettingsQuery"/> (single-resort — client KHÔNG gửi ResortId);
/// actor Publish = <see cref="ICurrentUser.UserId"/>. Map <c>Result</c>→HTTP qua <see cref="ProblemDetailsBuilder"/>
/// (nguồn shape lỗi DUY NHẤT — N-041). Preview/history dùng read use case D-Rules.3b (Req 8.4).
/// </summary>
public sealed class RulesAdminEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/rules", BedrockApiVersioning.V1);

        group.MapPost("/sections", CreateSectionAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesCreateSection");

        group.MapPut("/sections/{sectionId:guid}", UpdateSectionAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesUpdateSection");

        group.MapDelete("/sections/{sectionId:guid}", DeleteSectionAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesDeleteSection");

        group.MapPut("/sections/{sectionId:guid}/translations/{lang}", UpsertTranslationAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesUpsertTranslation");

        group.MapPost("/publish", PublishAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesPublish");

        // Xem trước Draft "như khách" (Req 8.4) — render theo lang, KHÔNG publish.
        group.MapGet("/preview", PreviewAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesPreview");

        // Lịch sử publication (Req 8.4) — metadata mọi bản đã phát hành.
        group.MapGet("/publications", PublicationsAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesPublications");

        group.MapGet("/admin", AdminDraftAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("RulesAdminDraft");
    }

    private static async Task<IResult> CreateSectionAsync(
        CreateRuleSectionRequest request,
        IUseCase<CreateRuleSectionInput, CreateRuleSectionResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resortId = await ResolveResortIdAsync(settingsQuery, http, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(RulesErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase
            .ExecuteAsync(new CreateRuleSectionInput(
                resortId.Value, request.Key, request.SortOrder, request.IsRequired, request.RequireScrollEnd, request.MinReadSeconds), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/rules/sections/{result.Value.SectionId}", new CreateRuleSectionResponse(result.Value.SectionId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid sectionId,
        UpdateRuleSectionRequest request,
        ICommandUseCase<UpdateRuleSectionInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new UpdateRuleSectionInput(
                sectionId, request.SortOrder, request.IsRequired, request.RequireScrollEnd, request.MinReadSeconds,
                request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> DeleteSectionAsync(
        Guid sectionId,
        uint expectedRowVersion,
        ICommandUseCase<DeleteRuleSectionInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new DeleteRuleSectionInput(sectionId, expectedRowVersion), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> UpsertTranslationAsync(
        Guid sectionId,
        string lang,
        UpsertRuleSectionTranslationRequest request,
        IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await useCase
            .ExecuteAsync(new UpsertRuleSectionTranslationInput(sectionId, lang, request.Title, request.BodyHtml, request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(new UpsertRuleSectionTranslationResponse(result.Value.TranslationId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> PublishAsync(
        PublishRulesRequest request,
        IUseCase<PublishRulesInput, PublishRulesResult> useCase,
        IResortSettingsQuery settingsQuery,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resortId = await ResolveResortIdAsync(settingsQuery, http, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(RulesErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase
            .ExecuteAsync(new PublishRulesInput(resortId.Value, currentUser.UserId, request.ChangeNote), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Ok(new PublishRulesResponse(result.Value.PublicationId, result.Value.Version))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> PreviewAsync(
        IUseCase<GetDraftPreviewInput, GetDraftPreviewResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct,
        string? lang = null)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, http, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(RulesErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase.ExecuteAsync(new GetDraftPreviewInput(resortId.Value, lang), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, http);
    }

    private static async Task<IResult> PublicationsAsync(
        IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, http, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(RulesErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase.ExecuteAsync(new GetPublicationHistoryInput(resortId.Value), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, http);
    }

    private static async Task<IResult> AdminDraftAsync(
        IUseCase<GetRuleAdminDraftInput, GetRuleAdminDraftResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, http, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(RulesErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase.ExecuteAsync(new GetRuleAdminDraftInput(resortId.Value), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, http);
    }

    private static async Task<Guid?> ResolveResortIdAsync(
        IResortSettingsQuery settingsQuery, HttpContext http, CancellationToken ct)
    {
        _ = http;
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        return settings?.ResortId;
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
