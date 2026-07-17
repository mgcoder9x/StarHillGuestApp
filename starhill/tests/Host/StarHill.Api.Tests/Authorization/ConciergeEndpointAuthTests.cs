using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bedrock.Api.Authentication;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Api;
using Concierge.Application;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard ánh xạ role endpoint module Concierge (K-Con.3): admin <c>/v1/conversations/*</c> + <c>/v1/notes/*</c> =
/// RequireStaff (Staff→2xx; no-token→401); guest <c>/v1/guest/messages</c> + <c>/v1/guest/conversation</c> =
/// AllowAnonymous (resolve context; resolver-fail→ProblemDetails, KHÔNG 401). TestServer map module THẬT + fake use
/// case/reader (KHÔNG DB). Mirror HousekeepingEndpointAuthTests.
/// </summary>
public sealed class ConciergeEndpointAuthTests
{
    private static readonly Guid ConversationId = FakeSendGuestMessage.ConversationId;
    private static readonly Guid NoteId = FakeCreateInternalNote.NoteId;

    private static async Task<IHost> StartAsync(FakeCurrentGuestContextResolver resolver)
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockApiVersioning();
                services.AddBedrockAuthCore(JwtTestTokens.BuildConfig());
                services.AddStarHillAuthorization();
                services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddSingleton<ICurrentGuestContextResolver>(resolver);
                services.AddScoped<IConciergeReader, FakeConciergeReader>();

                // Guest use case.
                services.AddScoped<IUseCase<SendGuestMessageInput, SendGuestMessageResult>, FakeSendGuestMessage>();
                services.AddScoped<IUseCase<GetGuestConversationInput, GetGuestConversationResult>, FakeGetGuestConversation>();
                // Staff use case.
                services.AddScoped<IUseCase<ReplyConversationInput, ReplyConversationResult>, FakeReplyConversation>();
                services.AddScoped<ICommandUseCase<MarkConversationReadInput>, FakeMarkConversationRead>();
                services.AddScoped<ICommandUseCase<CloseConversationInput>, FakeCloseConversation>();
                // Notes use case.
                services.AddScoped<IUseCase<CreateInternalNoteInput, CreateInternalNoteResult>, FakeCreateInternalNote>();
                services.AddScoped<ICommandUseCase<UpdateInternalNoteInput>, FakeUpdateInternalNote>();
                services.AddScoped<ICommandUseCase<DeleteInternalNoteInput>, FakeDeleteInternalNote>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    new ConciergeAdminEndpointModule().MapEndpoints(endpoints);
                    new ConciergeGuestEndpointModule().MapEndpoints(endpoints);
                });
            });
        });

        return await builder.StartAsync();
    }

    private static HttpClient Client(IHost host, string? role)
    {
        var client = host.GetTestClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(role));
        }

        return client;
    }

    private static FakeCurrentGuestContextResolver OkResolver() => new()
    {
        NextResult = Result.Success(new CurrentGuestContext(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), FakeResortSettingsQuery.ResortId)),
    };

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Staff_can_operate_conversations_and_notes()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel("/v1/conversations"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel($"/v1/conversations/{ConversationId}"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel($"/v1/conversations/{ConversationId}/reply"), JsonContent.Create(new { body = "xin chao" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync(Rel($"/v1/conversations/{ConversationId}/read"), null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync(Rel($"/v1/conversations/{ConversationId}/close"), null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/notes"), JsonContent.Create(new { roomId = Guid.CreateVersion7(), body = "ghi chu" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsync(Rel($"/v1/notes/{NoteId}"), JsonContent.Create(new { body = "sua" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync(Rel($"/v1/notes/{NoteId}"))).StatusCode);
    }

    [Fact]
    public async Task Missing_token_unauthorized_on_admin()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Rel("/v1/conversations"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel($"/v1/conversations/{ConversationId}/reply"), JsonContent.Create(new { body = "x" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/notes"), JsonContent.Create(new { roomId = Guid.CreateVersion7(), body = "x" }))).StatusCode);
    }

    [Fact]
    public async Task Guest_can_send_and_view_without_token()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/guest/messages"), JsonContent.Create(new { roomId = Guid.CreateVersion7(), body = "xin chao" }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync(Rel($"/v1/guest/conversation?roomId={Guid.CreateVersion7()}"))).StatusCode);
    }

    [Fact]
    public async Task Guest_resolver_failure_returns_problem_not_unauthorized()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Failure<CurrentGuestContext>(Error.Validation("guest_context_missing", "no cookie")),
        };
        using var host = await StartAsync(resolver);
        var client = Client(host, role: null);

        var response = await client.PostAsync(
            Rel("/v1/guest/messages"), JsonContent.Create(new { roomId = Guid.CreateVersion7(), body = "xin chao" }));
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
