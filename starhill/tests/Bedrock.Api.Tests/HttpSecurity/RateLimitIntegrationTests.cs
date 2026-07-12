using System.Net;
using Bedrock.Api;
using Bedrock.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bedrock.Api.Tests.HttpSecurity;

/// <summary>
/// Task 11.1 (F16) — rate-limit biên partition theo IP client THẬT: X-Forwarded-For (qua proxy tin cậy) →
/// ForwardedHeaders resolve RemoteIpAddress → rate-limiter partition theo IP đó. Chứng minh chuỗi đầy-đủ +
/// hai IP khác nhau = hai bucket độc lập (abuse một IP không ảnh hưởng IP khác).
/// </summary>
public sealed class RateLimitIntegrationTests
{
    private sealed class PingEndpoint : IEndpointModule
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
            endpoints.MapGet("/rl", () => Results.Ok("ok"));
    }

    private static async Task<IHost> StartAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpSecurity:KnownProxies:0"] = "10.0.0.1", // proxy tin cậy (peer) → chấp nhận X-Forwarded-For.
                ["HttpSecurity:RateLimitPermitLimit"] = "2",
                ["HttpSecurity:RateLimitWindowSeconds"] = "60",
                ["Jwt:ActiveKid"] = "k1",
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:Keys:0:Kid"] = "k1",
                ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            })
            .Build();

        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddBedrockApi(configuration);
                services.AddSingleton<IEndpointModule, PingEndpoint>();
            });
            web.Configure(app =>
            {
                // Giả lập peer = proxy tin cậy để ForwardedHeaders (#1) chấp nhận X-Forwarded-For trong TestServer.
                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
                    await next();
                });
                app.UseBedrockApi();
            });
        });

        return await builder.StartAsync();
    }

    [Fact]
    public async Task Rate_limit_partitions_by_forwarded_client_ip()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        // Cùng IP 3.3.3.3, PermitLimit=2 → request thứ 3 bị chặn 429.
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "3.3.3.3")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "3.3.3.3")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "3.3.3.3")).StatusCode);

        // IP khác 4.4.4.4 = bucket độc lập → vẫn OK (không bị ảnh hưởng bởi 3.3.3.3).
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "4.4.4.4")).StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string forwardedFor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/rl", UriKind.Relative));
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await client.SendAsync(request);
    }
}
