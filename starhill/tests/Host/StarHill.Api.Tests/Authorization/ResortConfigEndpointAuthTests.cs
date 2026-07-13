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
using ResortConfig.Api;
using ResortConfig.Application;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard ánh xạ role endpoint ResortConfig settings (Req 9.3, QR-AD-023): GET + PUT <c>/v1/resort/settings</c> đều
/// <see cref="StarHillPolicies.RequireAdmin"/>. TestServer map <see cref="ResortConfigEndpointModule"/> THẬT + fake
/// (KHÔNG DB/Docker): Admin GET=200 / PUT=204; Staff→403; no-token→401.
/// </summary>
public sealed class ResortConfigEndpointAuthTests
{
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
                services.AddScoped<ICommandUseCase<UpdateResortSettingsInput>, FakeUpdateResortSettings>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => new ResortConfigEndpointModule().MapEndpoints(endpoints));
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

    private static Uri Rel() => new("/v1/resort/settings", UriKind.Relative);

    private static object ValidBody() => new
    {
        faqEnabled = true,
        chatEnabled = true,
        housekeepingEnabled = true,
        requireRuleAckForFaq = false,
        requireRuleAckForChat = false,
        requireRuleAckForHousekeeping = false,
        portalWindowMinutes = 30,
        visitIdleExpiryHours = 24,
        guestWebBaseUrl = "https://guest.example.com",
        maxMessageLength = 2000,
        messageRateLimitPerMinute = 10,
        housekeepingRateLimitPerHour = 12,
    };

    [Fact]
    public async Task Admin_can_get_and_update_settings()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleAdmin);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel())).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync(Rel(), JsonContent.Create(ValidBody()))).StatusCode);
    }

    [Fact]
    public async Task Staff_forbidden_on_settings()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Rel())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsync(Rel(), JsonContent.Create(ValidBody()))).StatusCode);
    }

    [Fact]
    public async Task Missing_token_unauthorized_on_settings()
    {
        using var host = await StartAsync();
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Rel())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsync(Rel(), JsonContent.Create(ValidBody()))).StatusCode);
    }
}
