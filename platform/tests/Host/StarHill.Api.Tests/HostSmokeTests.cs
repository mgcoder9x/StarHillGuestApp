using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Smoke test COMPOSITION ROOT (task 16.2, R2): boot Host thật qua <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// Chứng minh toàn bộ compose ráp được + fail-fast DI (ValidateOnBuild/ValidateScopes) + RequiredPortsValidator
/// (port bảo mật bắt buộc có đủ) + duplicate-guard đều PASS lúc boot; endpoint module Identity được discovery/map
/// và pipeline behaviors (§8) bọc use case (Validation short-circuit → 400, KHÔNG chạm DB).
/// </summary>
public sealed class HostSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostSmokeTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    [Fact]
    public async Task Host_boots_and_liveness_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Identity_refresh_endpoint_is_mapped_and_pipeline_validates()
    {
        var client = _factory.CreateClient();

        // RefreshToken rỗng → ValidationUseCaseDecorator short-circuit → 400 (KHÔNG chạm DB).
        // Chứng minh: (a) endpoint module Identity được map; (b) pipeline behaviors bọc use case; (c) Result→ProblemDetails.
        var response = await client.PostAsJsonAsync(
            new Uri("/identity/token/refresh", UriKind.Relative),
            new { refreshToken = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
