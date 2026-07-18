using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bedrock.Api.Authentication;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using Rules.Api;
using Rules.Application;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard ánh xạ role endpoint ADMIN Rules (D-Rules.4c-1, Req 8/11.3): tất cả <c>/v1/rules/*</c> = RequireStaff.
/// TestServer map <see cref="RulesAdminEndpointModule"/> THẬT + fake use case (KHÔNG DB): Staff→2xx; no-token→401.
/// </summary>
public sealed class RulesAdminEndpointAuthTests
{
    private static readonly Guid SectionId = FakeCreateRuleSection.SectionId;

    private static async Task<IHost> StartAsync()
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

                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddScoped<IUseCase<CreateRuleSectionInput, CreateRuleSectionResult>, FakeCreateRuleSection>();
                services.AddScoped<ICommandUseCase<UpdateRuleSectionInput>, FakeUpdateRuleSection>();
                services.AddScoped<ICommandUseCase<DeleteRuleSectionInput>, FakeDeleteRuleSection>();
                services.AddScoped<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>, FakeUpsertRuleTranslation>();
                services.AddScoped<IUseCase<PublishRulesInput, PublishRulesResult>, FakePublishRules>();
                services.AddScoped<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>, FakeGetDraftPreview>();
                services.AddScoped<IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>, FakeGetPublicationHistory>();
                services.AddScoped<IUseCase<GetRuleAdminDraftInput, GetRuleAdminDraftResult>, FakeGetRuleAdminDraft>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => new RulesAdminEndpointModule().MapEndpoints(endpoints));
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

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Staff_can_author_and_publish_rules()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsync(Rel("/v1/rules/sections"), JsonContent.Create(new { key = "welcome", sortOrder = 1, isRequired = true, requireScrollEnd = false, minReadSeconds = 0 }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsync(Rel($"/v1/rules/sections/{SectionId}"), JsonContent.Create(new { sortOrder = 2, isRequired = true, requireScrollEnd = true, minReadSeconds = 5, expectedRowVersion = 0 }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsync(Rel($"/v1/rules/sections/{SectionId}/translations/en"), JsonContent.Create(new { title = "Welcome", bodyHtml = "<p>hi</p>", expectedRowVersion = (uint?)null }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync(Rel($"/v1/rules/sections/{SectionId}?expectedRowVersion=0"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/rules/publish"), JsonContent.Create(new { changeNote = "first" }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync(Rel("/v1/rules/preview?lang=en"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync(Rel("/v1/rules/publications"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync(Rel("/v1/rules/admin"))).StatusCode);
    }

    [Fact]
    public async Task Missing_token_unauthorized()
    {
        using var host = await StartAsync();
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/rules/sections"), JsonContent.Create(new { key = "x", sortOrder = 1, isRequired = false, requireScrollEnd = false, minReadSeconds = 0 }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/rules/publish"), JsonContent.Create(new { changeNote = (string?)null }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Rel("/v1/rules/preview?lang=en"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Rel("/v1/rules/publications"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Rel("/v1/rules/admin"))).StatusCode);
    }
}
