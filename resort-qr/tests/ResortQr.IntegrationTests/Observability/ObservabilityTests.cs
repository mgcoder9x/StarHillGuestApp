using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ResortQr.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ResortQr.IntegrationTests.Observability;

public sealed class ObservabilityTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public ObservabilityTests(AuthApiFactory factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    [Fact] // Req 14.3: /health/live không phụ thuộc DB → 200
    public async Task Health_live_should_return_200()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact] // 19 §2: mọi response có X-Correlation-Id
    public async Task Response_should_carry_correlation_id_header()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Correlation-Id").First()));
    }

    [Fact] // 19 §2: nếu client gửi X-Correlation-Id hợp lệ → echo lại (đối soát)
    public async Task Provided_correlation_id_should_be_echoed()
    {
        var client = CreateClient();
        const string provided = "test-correlation-123";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", provided);
        var response = await client.SendAsync(request);

        Assert.Equal(provided, response.Headers.GetValues("X-Correlation-Id").First());
    }
}
