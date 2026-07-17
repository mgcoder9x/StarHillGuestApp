using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Application;
using Concierge.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;

namespace Concierge.Api;

// ---- DTO Api (client KHÔNG set ResortId/actor; server phân giải từ ICurrentUser/IResortSettingsQuery). ----
public sealed record ReplyConversationRequest(string Body);

public sealed record ReplyConversationResponse(Guid MessageId, ConversationStatus Status);

public sealed record CreateInternalNoteRequest(Guid? RoomId, Guid? ConversationId, string Body);

public sealed record CreateInternalNoteResponse(Guid NoteId);

public sealed record UpdateInternalNoteRequest(string Body);

/// <summary>
/// Nửa-Api ADMIN module Concierge (K-Con.3): board hội thoại + chi tiết + reply/read/close + ghi chú nội bộ CRUD.
/// TẤT CẢ <see cref="StarHillPolicies.RequireStaff"/> (Req 5.3/9.4 — nhân viên; ghi chú KHÔNG BAO GIỜ lộ cho khách).
/// Actor = <see cref="ICurrentUser.UserId"/> (ghi StaffUserId/AuthorUserId/ClosedByUserId — QR-DV-004; guard null →
/// <c>unauthorized</c>, dù RequireStaff đã đảm bảo). ResortId phân giải server qua <see cref="IResortSettingsQuery"/>
/// (board + tạo note). Board đọc qua <see cref="IConciergeReader"/> (order LastMessageAt DESC — QR-TO-014). Mirror
/// HousekeepingAdminEndpointModule.
/// </summary>
public sealed class ConciergeAdminEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var conversations = endpoints.MapVersionedGroup("/conversations", BedrockApiVersioning.V1);
        conversations.MapGet("/", BoardAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeBoard");
        conversations.MapGet("/{conversationId:guid}", DetailAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeConversationDetail");
        conversations.MapPost("/{conversationId:guid}/reply", ReplyAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeReply");
        conversations.MapPost("/{conversationId:guid}/read", MarkReadAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeMarkRead");
        conversations.MapPost("/{conversationId:guid}/close", CloseAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeClose");

        var notes = endpoints.MapVersionedGroup("/notes", BedrockApiVersioning.V1);
        notes.MapPost("/", CreateNoteAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeCreateNote");
        notes.MapPut("/{noteId:guid}", UpdateNoteAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeUpdateNote");
        notes.MapDelete("/{noteId:guid}", DeleteNoteAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff).MapToApiVersion(BedrockApiVersioning.V1).WithName("ConciergeDeleteNote");
    }

    private static async Task<IResult> BoardAsync(
        IConciergeReader reader,
        IResortSettingsQuery settingsQuery,
        HttpContext http,
        CancellationToken ct,
        ConversationStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(ConciergeErrors.ConfigurationUnavailable, http);
        }

        var board = await reader.ListConversationsAsync(resortId.Value, status, page, pageSize, ct).ConfigureAwait(false);
        return Results.Ok(board);
    }

    private static async Task<IResult> DetailAsync(
        Guid conversationId,
        IConciergeReader reader,
        HttpContext http,
        CancellationToken ct)
    {
        var detail = await reader.GetConversationAsync(conversationId, ct).ConfigureAwait(false);
        return detail is null ? Problem(ConciergeErrors.ConversationNotFound, http) : Results.Ok(detail);
    }

    private static async Task<IResult> ReplyAsync(
        Guid conversationId,
        ReplyConversationRequest request,
        IUseCase<ReplyConversationInput, ReplyConversationResult> useCase,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (currentUser.UserId is not { } staffId)
        {
            return Problem(CommonErrors.Unauthorized(), http);
        }

        var result = await useCase
            .ExecuteAsync(new ReplyConversationInput(conversationId, staffId, request.Body), ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new ReplyConversationResponse(result.Value.MessageId, result.Value.Status))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid conversationId,
        ICommandUseCase<MarkConversationReadInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new MarkConversationReadInput(conversationId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> CloseAsync(
        Guid conversationId,
        ICommandUseCase<CloseConversationInput> useCase,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        if (currentUser.UserId is not { } staffId)
        {
            return Problem(CommonErrors.Unauthorized(), http);
        }

        var result = await useCase.ExecuteAsync(new CloseConversationInput(conversationId, staffId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> CreateNoteAsync(
        CreateInternalNoteRequest request,
        IUseCase<CreateInternalNoteInput, CreateInternalNoteResult> useCase,
        IResortSettingsQuery settingsQuery,
        ICurrentUser currentUser,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (currentUser.UserId is not { } authorId)
        {
            return Problem(CommonErrors.Unauthorized(), http);
        }

        var resortId = await ResolveResortIdAsync(settingsQuery, ct).ConfigureAwait(false);
        if (resortId is null)
        {
            return Problem(ConciergeErrors.ConfigurationUnavailable, http);
        }

        var result = await useCase
            .ExecuteAsync(
                new CreateInternalNoteInput(resortId.Value, request.RoomId, request.ConversationId, authorId, request.Body),
                ct)
            .ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(new CreateInternalNoteResponse(result.Value.NoteId))
            : Problem(result.Error, http);
    }

    private static async Task<IResult> UpdateNoteAsync(
        Guid noteId,
        UpdateInternalNoteRequest request,
        ICommandUseCase<UpdateInternalNoteInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await useCase.ExecuteAsync(new UpdateInternalNoteInput(noteId, request.Body), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<IResult> DeleteNoteAsync(
        Guid noteId,
        ICommandUseCase<DeleteInternalNoteInput> useCase,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(new DeleteInternalNoteInput(noteId), ct).ConfigureAwait(false);
        return result.IsSuccess ? Results.NoContent() : Problem(result.Error, http);
    }

    private static async Task<Guid?> ResolveResortIdAsync(IResortSettingsQuery settingsQuery, CancellationToken ct)
    {
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        return settings?.ResortId;
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
