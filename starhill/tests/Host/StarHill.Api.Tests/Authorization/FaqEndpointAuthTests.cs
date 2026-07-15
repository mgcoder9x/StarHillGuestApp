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
using Faq.Api;
using Faq.Application;
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
/// Guard ánh xạ role endpoint module Faq (E-Faq.4): admin <c>/v1/faq/*</c> = RequireStaff (Staff→2xx; no-token→401);
/// guest <c>/v1/guest/faq</c> = AllowAnonymous (resolve ngữ cảnh khách; resolver-fail→ProblemDetails, KHÔNG 401).
/// TestServer map module THẬT + fake use case (KHÔNG DB).
/// </summary>
public sealed class FaqEndpointAuthTests
{
    private static readonly Guid CategoryId = FakeCreateFaqCategory.CategoryId;
    private static readonly Guid ItemId = FakeCreateFaqItem.ItemId;

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

                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddSingleton<ICurrentGuestContextResolver>(resolver);

                services.AddScoped<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>, FakeCreateFaqCategory>();
                services.AddScoped<ICommandUseCase<UpdateFaqCategoryInput>, FakeUpdateFaqCategory>();
                services.AddScoped<ICommandUseCase<DeleteFaqCategoryInput>, FakeDeleteFaqCategory>();
                services.AddScoped<IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>, FakeUpsertFaqCategoryTranslation>();
                services.AddScoped<IUseCase<CreateFaqItemInput, CreateFaqItemResult>, FakeCreateFaqItem>();
                services.AddScoped<ICommandUseCase<UpdateFaqItemInput>, FakeUpdateFaqItem>();
                services.AddScoped<ICommandUseCase<DeleteFaqItemInput>, FakeDeleteFaqItem>();
                services.AddScoped<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>, FakeUpsertFaqItemTranslation>();
                services.AddScoped<ICommandUseCase<ReorderFaqCategoriesInput>, FakeReorderFaqCategories>();
                services.AddScoped<ICommandUseCase<ReorderFaqItemsInput>, FakeReorderFaqItems>();
                services.AddScoped<IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>, FakeGetGuestFaqTree>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    new FaqAdminEndpointModule().MapEndpoints(endpoints);
                    new FaqGuestEndpointModule().MapEndpoints(endpoints);
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
            GuestVisitId: Guid.CreateVersion7(),
            GuestSessionId: Guid.CreateVersion7(),
            RoomId: Guid.CreateVersion7(),
            ResortId: FakeResortSettingsQuery.ResortId)),
    };

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Staff_can_manage_faq()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsync(Rel("/v1/faq/categories"), JsonContent.Create(new { key = "arrival", sortOrder = 1, isActive = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsync(Rel($"/v1/faq/categories/{CategoryId}"), JsonContent.Create(new { sortOrder = 2, isActive = false }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsync(Rel($"/v1/faq/categories/{CategoryId}/translations/en"), JsonContent.Create(new { name = "Arrival" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsync(Rel("/v1/faq/items"), JsonContent.Create(new { categoryId = CategoryId, parentId = (Guid?)null, sortOrder = 1, isActive = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsync(Rel($"/v1/faq/items/{ItemId}"), JsonContent.Create(new { parentId = (Guid?)null, sortOrder = 2, isActive = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsync(Rel($"/v1/faq/items/{ItemId}/translations/en"), JsonContent.Create(new { question = "Q", answerHtml = "<p>A</p>" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync(Rel("/v1/faq/reorder/categories"), JsonContent.Create(new { entries = new[] { new { id = CategoryId, sortOrder = 1 } } }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync(Rel($"/v1/faq/reorder/items/{CategoryId}"), JsonContent.Create(new { entries = new[] { new { id = ItemId, sortOrder = 1 } } }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync(Rel($"/v1/faq/items/{ItemId}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync(Rel($"/v1/faq/categories/{CategoryId}"))).StatusCode);
    }

    [Fact]
    public async Task Missing_token_unauthorized_on_admin()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/faq/categories"), JsonContent.Create(new { key = "x", sortOrder = 1, isActive = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/faq/items"), JsonContent.Create(new { categoryId = CategoryId, parentId = (Guid?)null, sortOrder = 1, isActive = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/faq/reorder/categories"), JsonContent.Create(new { entries = Array.Empty<object>() }))).StatusCode);
    }

    [Fact]
    public async Task Guest_can_read_faq_tree_without_token()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null); // AllowAnonymous — cookie thiết bị (fake resolver trả context).

        var response = await client.GetAsync(Rel($"/v1/guest/faq?roomId={Guid.CreateVersion7()}&lang=en"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Guest_resolver_failure_returns_problem_not_unauthorized()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Failure<CurrentGuestContext>(
                Error.Validation("guest_context_missing", "no cookie")),
        };
        using var host = await StartAsync(resolver);
        var client = Client(host, role: null);

        var response = await client.GetAsync(Rel($"/v1/guest/faq?roomId={Guid.CreateVersion7()}"));
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode); // AllowAnonymous → không 401
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
