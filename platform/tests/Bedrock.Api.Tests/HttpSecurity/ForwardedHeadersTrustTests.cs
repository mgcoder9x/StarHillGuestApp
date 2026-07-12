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
/// P0-01 (re-audit 2026-07-12) — KIỂM CHỨNG HÀNH VI THẬT của Forwarded Headers trust policy bằng TestServer +
/// <c>RemoteIpAddress</c> cụ thể (không chỉ test options). Câu hỏi: khi <c>KnownProxies/KnownNetworks</c> RỖNG,
/// client gọi TRỰC TIẾP đặt <c>X-Forwarded-For</c> có SPOOF được IP quan sát không?
/// <para>
/// Kỳ vọng (.NET ≥ 8.0.17/9.0.6, gồm .NET 10 — security hardening): header từ nguồn KHÔNG được cấu hình tin cậy
/// bị BỎ QUA → <c>RemoteIpAddress</c> giữ IP kết nối thật (KHÔNG spoof được). Proxy được khai tin cậy → honor XFF.
/// </para>
/// </summary>
public sealed class ForwardedHeadersTrustTests
{
    private const string UntrustedClientIp = "203.0.113.9"; // TEST-NET-3, KHÔNG loopback → không auto-trust.
    private const string SpoofedForwardedIp = "1.2.3.4";

    private sealed class WhoAmIEndpoint : IEndpointModule
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
            endpoints.MapGet("/whoami", (HttpContext ctx) =>
                Results.Text(ctx.Connection.RemoteIpAddress?.ToString() ?? "null"));
    }

    private static async Task<IHost> StartAsync(string? knownProxy)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:ActiveKid"] = "k1",
            ["Jwt:Issuer"] = "iss",
            ["Jwt:Audience"] = "aud",
            ["Jwt:Keys:0:Kid"] = "k1",
            ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
        };
        if (knownProxy is not null)
        {
            settings["HttpSecurity:KnownProxies:0"] = knownProxy;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var builder = new HostBuilder().ConfigureWebHost(web =>
        {
            web.UseTestServer();
            web.ConfigureServices(services =>
            {
                services.AddBedrockApi(configuration);
                services.AddSingleton<IEndpointModule, WhoAmIEndpoint>();
            });
            web.Configure(app =>
            {
                // Peer (RemoteIpAddress kết nối) = client gọi TRỰC TIẾP, đặt TRƯỚC UseBedrockApi (#1 ForwardedHeaders).
                app.Use(async (context, next) =>
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(UntrustedClientIp);
                    await next();
                });
                app.UseBedrockApi();
            });
        });

        return await builder.StartAsync();
    }

    private static async Task<string> WhoAmIAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/whoami", UriKind.Relative));
        request.Headers.Add("X-Forwarded-For", SpoofedForwardedIp);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Untrusted_peer_cannot_spoof_ip_via_forwarded_header()
    {
        using var host = await StartAsync(knownProxy: null); // KnownProxies rỗng (mặc định base).
        var observed = await WhoAmIAsync(host.GetTestClient());

        // Header từ nguồn KHÔNG tin cậy bị BỎ QUA → giữ IP kết nối thật (không spoof). KHÔNG được là IP giả.
        Assert.Equal(UntrustedClientIp, observed);
        Assert.NotEqual(SpoofedForwardedIp, observed);
    }

    [Fact]
    public async Task Trusted_proxy_forwarded_header_is_honored()
    {
        using var host = await StartAsync(knownProxy: UntrustedClientIp); // khai peer là proxy tin cậy.
        var observed = await WhoAmIAsync(host.GetTestClient());

        // Peer tin cậy → XFF được honor → RemoteIpAddress = IP client thật do proxy chuyển tiếp.
        Assert.Equal(SpoofedForwardedIp, observed);
    }
}
