using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Factory boot Host với SECRET nạp qua test-config (KHÔNG lấy từ repo — F35/task 19): <c>appsettings.json</c>
/// chỉ chứa non-secret + placeholder (Jwt secret rỗng, connection string không mật khẩu), nên test PHẢI tự cấp
/// secret giống dev nạp qua User-Secrets. Đây là cách đúng: secret sống ngoài repo, mỗi môi trường tự cấp.
/// </summary>
public sealed class SecretInjectingHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Secret HS256 hợp lệ (≥32 byte) — cấp lúc test như dev cấp qua User-Secrets.
            ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
            // Connection string đầy đủ (kèm password) — cấp lúc test; smoke test không chạm DB thật.
            ["ConnectionStrings:Identity"] = "Host=localhost;Port=5432;Database=starhill_identity_test;Username=postgres;Password=test",
        }));
    }
}

/// <summary>
/// Smoke test COMPOSITION ROOT (task 16.2/19, R2/R25): boot Host thật qua <see cref="WebApplicationFactory{TEntryPoint}"/>
/// với secret nạp NGOÀI repo. Chứng minh compose ráp được + fail-fast DI + RequiredPortsValidator + duplicate-guard
/// PASS lúc boot; endpoint module Identity discovery/map dưới /v1 + pipeline behaviors bọc use case. Test fail-fast
/// khi THIẾU secret bắt buộc (F35) nằm cùng file.
/// </summary>
public sealed class HostSmokeTests : IClassFixture<SecretInjectingHostFactory>
{
    private readonly SecretInjectingHostFactory _factory;

    public HostSmokeTests(SecretInjectingHostFactory factory)
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

        // Route versioned (F32): /v1/... . RefreshToken rỗng → ValidationUseCaseDecorator short-circuit → 400
        // (KHÔNG chạm DB). Chứng minh: (a) endpoint module Identity map dưới /v1; (b) pipeline behaviors bọc use case;
        // (c) Result→ProblemDetails; (d) versioning active.
        var response = await client.PostAsJsonAsync(
            new Uri("/v1/identity/token/refresh", UriKind.Relative),
            new { refreshToken = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // ReportApiVersions=true → endpoint versioned trả header liệt kê version hỗ trợ.
        Assert.True(response.Headers.Contains("api-supported-versions"));
        Assert.Contains("1.0", string.Join(",", response.Headers.GetValues("api-supported-versions")), StringComparison.Ordinal);
    }

    [Fact]
    public void Host_fails_fast_when_required_jwt_secret_is_missing()
    {
        // Chỉ cấp connection string, KHÔNG cấp Jwt secret → appsettings để trống (F35: không secret trong repo)
        // → validate-on-start (AddBedrockSecurity) PHẢI chặn boot. Test này VỪA chứng minh fail-fast (R25.2)
        // VỪA là guard "appsettings KHÔNG chứa JWT secret" (nếu có secret thật trong repo, boot đã KHÔNG fail).
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=x;Username=postgres;Password=test",
            })));

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        var text = Flatten(exception);
        Assert.Contains("HS256", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Flatten(Exception exception)
    {
        var text = string.Empty;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            text += current.Message + " ";
        }

        return text;
    }
}
