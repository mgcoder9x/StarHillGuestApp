using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using Faq.Application;
using StarHill.Authorization;

namespace Faq.Api;

// ---- DTO Api (tách khỏi command Application — client KHÔNG set ResortId; server phân giải single-resort). ----
public sealed record CreateFaqCategoryRequest(string Key, int SortOrder, bool IsActive);

public sealed record CreateFaqCategoryResponse(Guid CategoryId);

public sealed record UpdateFaqCategoryRequest(int SortOrder, bool IsActive, uint ExpectedRowVersion);

public sealed record UpsertFaqCategoryTranslationRequest(string? Name, uint? ExpectedRowVersion);

public sealed record UpsertFaqTranslationResponse(Guid TranslationId);

public sealed record CreateFaqItemRequest(Guid CategoryId, Guid? ParentId, int SortOrder, bool IsActive);

public sealed record CreateFaqItemResponse(Guid ItemId);

public sealed record UpdateFaqItemRequest(Guid? ParentId, int SortOrder, bool IsActive, uint ExpectedRowVersion);

/// <summary>Body upsert bản dịch item — <c>Question</c>/<c>AnswerHtml</c> THÔ; use case sanitize trước lưu (CP12).</summary>
public sealed record UpsertFaqItemTranslationRequest(string? Question, string? AnswerHtml, uint? ExpectedRowVersion);

public sealed record FaqReorderEntryDto(Guid Id, int SortOrder, uint ExpectedRowVersion);

public sealed record FaqReorderRequest(IReadOnlyList<FaqReorderEntryDto> Entries);

/// <summary>
/// Nửa-Api ADMIN module Faq (E-Faq.4): CRUD danh mục/mục + bản dịch (sanitize-on-save) + reorder. Tất cả
/// <see cref="StarHillPolicies.RequireStaff"/> (Req 8.5 — FAQ thuộc quyền Staff; Admin superset). ResortId phân giải
/// SERVER-SIDE qua <see cref="IResortSettingsQuery"/> (single-resort — client KHÔNG gửi ResortId). Map <c>Result</c>→HTTP
/// qua <see cref="ProblemDetailsBuilder"/>. Admin READ-tree E-Faq.4b trả full/inactive/raw-translations + xmin.
/// </summary>
public sealed class FaqAdminEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/faq", BedrockApiVersioning.V1);

        group.MapGet("/admin", GetAdminTreeAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqAdminTree");

        group.MapPost("/categories", CreateCategoryAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqCreateCategory");
        group.MapPut("/categories/{categoryId:guid}", UpdateCategoryAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqUpdateCategory");
        group.MapDelete("/categories/{categoryId:guid}", DeleteCategoryAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqDeleteCategory");
        group.MapPut("/categories/{categoryId:guid}/translations/{lang}", UpsertCategoryTranslationAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqUpsertCategoryTranslation");

        group.MapPost("/items", CreateItemAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqCreateItem");
        group.MapPut("/items/{itemId:guid}", UpdateItemAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqUpdateItem");
        group.MapDelete("/items/{itemId:guid}", DeleteItemAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqDeleteItem");
        group.MapPut("/items/{itemId:guid}/translations/{lang}", UpsertItemTranslationAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqUpsertItemTranslation");

        group.MapPost("/reorder/categories", ReorderCategoriesAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqReorderCategories");
        group.MapPost("/reorder/items/{categoryId:guid}", ReorderItemsAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("FaqReorderItems");
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateFaqCategoryRequest request,
        IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(FaqErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase
            .ExecuteAsync(new CreateFaqCategoryInput(resortId.Value, request.Key, request.SortOrder, request.IsActive), ct)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Results.Created($"/v1/faq/categories/{result.Value.CategoryId}", new CreateFaqCategoryResponse(result.Value.CategoryId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> GetAdminTreeAsync(
        IUseCase<GetFaqAdminTreeInput, GetFaqAdminTreeResult> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(FaqErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase.ExecuteAsync(new GetFaqAdminTreeInput(resortId.Value), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error, http);
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid categoryId,
        UpdateFaqCategoryRequest request,
        ICommandUseCase<UpdateFaqCategoryInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new UpdateFaqCategoryInput(categoryId, request.SortOrder, request.IsActive, request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> DeleteCategoryAsync(
        Guid categoryId,
        uint expectedRowVersion,
        ICommandUseCase<DeleteFaqCategoryInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new DeleteFaqCategoryInput(categoryId, expectedRowVersion), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> UpsertCategoryTranslationAsync(
        Guid categoryId,
        string lang,
        UpsertFaqCategoryTranslationRequest request,
        IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new UpsertFaqCategoryTranslationInput(categoryId, lang, request.Name, request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new UpsertFaqTranslationResponse(result.Value.TranslationId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> CreateItemAsync(
        CreateFaqItemRequest request,
        IUseCase<CreateFaqItemInput, CreateFaqItemResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new CreateFaqItemInput(request.CategoryId, request.ParentId, request.SortOrder, request.IsActive), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Created($"/v1/faq/items/{result.Value.ItemId}", new CreateFaqItemResponse(result.Value.ItemId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid itemId,
        UpdateFaqItemRequest request,
        ICommandUseCase<UpdateFaqItemInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new UpdateFaqItemInput(itemId, request.ParentId, request.SortOrder, request.IsActive, request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> DeleteItemAsync(
        Guid itemId,
        uint expectedRowVersion,
        ICommandUseCase<DeleteFaqItemInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new DeleteFaqItemInput(itemId, expectedRowVersion), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> UpsertItemTranslationAsync(
        Guid itemId,
        string lang,
        UpsertFaqItemTranslationRequest request,
        IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase
            .ExecuteAsync(new UpsertFaqItemTranslationInput(itemId, lang, request.Question, request.AnswerHtml, request.ExpectedRowVersion), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new UpsertFaqTranslationResponse(result.Value.TranslationId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> ReorderCategoriesAsync(
        FaqReorderRequest request,
        ICommandUseCase<ReorderFaqCategoriesInput> useCase,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(FaqErrors.ConfigurationUnavailable, http);
        }

        var entries = MapEntries(request);
        var result = await useCase
            .ExecuteAsync(new ReorderFaqCategoriesInput(resortId.Value, entries), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> ReorderItemsAsync(
        Guid categoryId,
        FaqReorderRequest request,
        ICommandUseCase<ReorderFaqItemsInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = MapEntries(request);
        var result = await useCase
            .ExecuteAsync(new ReorderFaqItemsInput(categoryId, entries), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static List<FaqReorderEntry> MapEntries(FaqReorderRequest request) =>
        (request.Entries ?? []).Select(e => new FaqReorderEntry(e.Id, e.SortOrder, e.ExpectedRowVersion)).ToList();

    private static async Task<Guid?> ResolveResortIdAsync(IResortSettingsQuery settingsQuery, CancellationToken ct)
    {
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        return settings?.ResortId;
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
