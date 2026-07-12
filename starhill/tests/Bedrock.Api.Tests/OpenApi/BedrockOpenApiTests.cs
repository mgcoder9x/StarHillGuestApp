using System.Net;
using Asp.Versioning.Builder;
using Bedrock.Api;
using Bedrock.Api.Endpoints;
using Bedrock.Api.OpenApi;
using Bedrock.Api.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bedrock.Api.Tests.OpenApi;

/// <summary>
/// Guard OpenAPI doc-gen của base (AD-068, R22.1/§9.1, hoàn tất phần base DV-015): native .NET 10 OpenAPI,
/// OPT-IN. (1) Host gọi <c>AddBedrockOpenApi</c> → <c>/openapi/v1.json</c> phục vụ + document endpoint versioned.
/// (2) KHÔNG opt-in → không map (404) → chứng minh opt-in đúng (không ép OpenAPI lên mọi Host — tinh thần DV-015).
/// </summary>
public sealed class BedrockOpenApiTests
{
    private sealed class VersionedEndpoints : IEndpointModule
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapVersionedGroup("/oa");
            group.MapGet("/ping", () => Results.Ok("pong")).MapToApiVersion(BedrockApiVersioning.V1);
        }
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ActiveKid"] = "k1",
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:Keys:0:Kid"] = "k1",
                ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            })
            .Build();

    private static async Task<IHost> StartAsync(bool withOpenApi)
    {
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddBedrockApi(Config());
                if (withOpenApi)
                {
                    services.AddBedrockOpenApi();
                }

                services.AddSingleton<IEndpointModule, VersionedEndpoints>();
            });
            web.Configure(app => app.UseBedrockApi());
        });
        return await builder.StartAsync();
    }

    [Fact]
    public async Task OpenApi_document_is_served_and_includes_versioned_endpoint_when_opted_in()
    {
        using var host = await StartAsync(withOpenApi: true);
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body, StringComparison.Ordinal); // root field của OpenAPI document.
        Assert.Contains("oa/ping", body, StringComparison.Ordinal);      // endpoint versioned được document (R22.1).
    }

    [Fact]
    public async Task OpenApi_document_is_not_mapped_when_not_opted_in()
    {
        using var host = await StartAsync(withOpenApi: false);
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // opt-in: không AddBedrockOpenApi → không map.
    }
}
