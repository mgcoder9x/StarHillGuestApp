using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Foundation.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Foundation.IntegrationTests.RateLimiting;

public sealed class RateLimitTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public RateLimitTests(AuthApiFactory factory) => _factory = factory;

    [Fact] // Req 13.4: vượt ngưỡng → 429 rate_limited (ProblemDetails)
    public async Task Exceeding_rate_limit_should_return_429_problem()
    {
        // Factory riêng với ngưỡng THẤP (2/60s) để không dính state của test khác.
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimit:PermitLimit"] = "2",
                    ["RateLimit:WindowSeconds"] = "60",
                })));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/auth/login", new { email = "x@x.com", password = "whatever12" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("application/problem+json", last.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await last.Content.ReadAsStringAsync());
        Assert.Equal("rate_limited", doc.RootElement.GetProperty("code").GetString());
    }
}
