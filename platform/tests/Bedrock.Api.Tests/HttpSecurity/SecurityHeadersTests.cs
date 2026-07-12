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
/// Guard slot #5 pipeline (§3.5, AD-067) — <c>SecurityHeadersMiddleware</c> áp bộ header phòng thủ cho MỌI
/// response, KỂ CẢ response lỗi (headers set qua <c>OnStarting</c> → có mặt trước khi body bắt đầu, cả trên 500).
/// Trước đây control BẢO MẬT này KHÔNG có guard test → xoá/đổi header sẽ không test nào bắt (drift bảo mật thầm lặng).
/// </summary>
public sealed class SecurityHeadersTests
{
    private sealed class Endpoints : IEndpointModule
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/ok", () => Results.Ok("ok"));
            endpoints.MapGet("/boom", IResult () => throw new InvalidOperationException("kaboom"));
        }
    }

    private static async Task<IHost> StartAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
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
                services.AddSingleton<IEndpointModule, Endpoints>();
            });
            web.Configure(app => app.UseBedrockApi());
        });
        return await builder.StartAsync();
    }

    [Theory]
    [InlineData("/ok", HttpStatusCode.OK)]                          // response thành công.
    [InlineData("/boom", HttpStatusCode.InternalServerError)]       // response lỗi — header VẪN phải có (OnStarting).
    public async Task Every_response_carries_defensive_security_headers(string path, HttpStatusCode expectedStatus)
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(expectedStatus, response.StatusCode);
        AssertHeader(response, "X-Content-Type-Options", "nosniff");           // chống MIME-sniffing.
        AssertHeader(response, "X-Frame-Options", "DENY");                     // chống clickjacking (không cho frame).
        AssertHeader(response, "Referrer-Policy", "no-referrer");              // không rò Referer.
        AssertHeader(response, "X-Permitted-Cross-Domain-Policies", "none");   // chặn Adobe cross-domain policy.
    }

    private static void AssertHeader(HttpResponseMessage response, string name, string expectedValue)
    {
        Assert.True(response.Headers.TryGetValues(name, out var values), $"thiếu security header '{name}'.");
        Assert.Equal(expectedValue, values!.Single());
    }
}
