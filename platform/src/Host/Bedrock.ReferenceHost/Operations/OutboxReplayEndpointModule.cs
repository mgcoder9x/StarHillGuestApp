using Asp.Versioning.Builder;
using Bedrock.Api.Endpoints;
using Bedrock.Api.Versioning;
using Bedrock.Application.Messaging.Dispatch;
using Identity.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.ReferenceHost.Operations;

internal static class OutboxReplayAuthorization
{
    public const string Policy = "platform-outbox-replay";
    public const string Permission = "platform.outbox.replay";
}

internal sealed record OutboxReplayHttpRequest(
    Guid OperationId,
    string Reason,
    IReadOnlyCollection<Guid>? MessageIds,
    string? EventType,
    DateTimeOffset? OccurredFrom,
    DateTimeOffset? OccurredTo,
    int MaxMessages = 100);

/// <summary>
/// Reference operator surface. Authorization and actor derivation live in the Host while replay selection, mutation,
/// single-use operation identity and durable audit remain reusable Application/Infrastructure capabilities.
/// </summary>
internal sealed class OutboxReplayEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/operations/outbox/replay", BedrockApiVersioning.V1)
            .RequireAuthorization(OutboxReplayAuthorization.Policy);

        group.MapPost("/preview", PreviewAsync)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("OutboxReplayPreview");
        group.MapPost(string.Empty, ExecuteAsync)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("OutboxReplayExecute");
        group.MapGet("/{operationId:guid}", GetAuditAsync)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("OutboxReplayAudit");
    }

    private static Task<IResult> PreviewAsync(
        OutboxReplayHttpRequest request,
        HttpContext http,
        [FromKeyedServices(IdentityInfrastructureExtensions.PersistenceKey)] IOutboxReplayService service,
        CancellationToken ct) => ExecuteCoreAsync(request, http, service, dryRun: true, ct);

    private static Task<IResult> ExecuteAsync(
        OutboxReplayHttpRequest request,
        HttpContext http,
        [FromKeyedServices(IdentityInfrastructureExtensions.PersistenceKey)] IOutboxReplayService service,
        CancellationToken ct) => ExecuteCoreAsync(request, http, service, dryRun: false, ct);

    private static async Task<IResult> ExecuteCoreAsync(
        OutboxReplayHttpRequest request,
        HttpContext http,
        IOutboxReplayService service,
        bool dryRun,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = http.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actor))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Authenticated operator identity is missing.");
        }

        try
        {
            var result = await service.ExecuteAsync(new OutboxReplayRequest
            {
                OperationId = request.OperationId,
                Actor = actor,
                Reason = request.Reason,
                MessageIds = request.MessageIds ?? [],
                EventType = request.EventType,
                OccurredFrom = request.OccurredFrom,
                OccurredTo = request.OccurredTo,
                MaxMessages = request.MaxMessages,
                DryRun = dryRun,
            }, ct).ConfigureAwait(false);
            return Results.Ok(result);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: exception.Message);
        }
    }

    private static async Task<IResult> GetAuditAsync(
        Guid operationId,
        [FromKeyedServices(IdentityInfrastructureExtensions.PersistenceKey)] IOutboxReplayAuditReader reader,
        CancellationToken ct)
    {
        var record = await reader.GetAsync(operationId, ct).ConfigureAwait(false);
        return record is null ? Results.NotFound() : Results.Ok(record);
    }
}
