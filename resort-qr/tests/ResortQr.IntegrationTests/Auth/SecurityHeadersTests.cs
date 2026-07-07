using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ResortQr.IntegrationTests.Auth;

public sealed class SecurityHeadersTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public SecurityHeadersTests(AuthApiFactory factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact] // Req 20.4: mọi response có CSP + X-Content-Type-Options
    public async Task Responses_should_carry_security_headers()
    {
        var client = CreateClient();

        // /auth/me không auth → 401, nhưng security header vẫn phải có mặt (middleware đặt sớm).
        var response = await client.GetAsync("/auth/me");

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", string.Join(" ", response.Headers.GetValues("Content-Security-Policy")), System.StringComparison.Ordinal);

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
    }

    [Fact] // Req 20.3: preflight từ origin được phép → có Access-Control-Allow-Origin
    public async Task Preflight_from_allowed_origin_should_be_permitted()
    {
        var client = CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        preflight.Headers.Add("Origin", "https://allowed.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(preflight);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://allowed.example", response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [Fact] // Req 20.2: origin KHÔNG được phép → không set Access-Control-Allow-Origin
    public async Task Preflight_from_disallowed_origin_should_not_be_permitted()
    {
        var client = CreateClient();

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/auth/login");
        preflight.Headers.Add("Origin", "https://evil.example");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(preflight);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
