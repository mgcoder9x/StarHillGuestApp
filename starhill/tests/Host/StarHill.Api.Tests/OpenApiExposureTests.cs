using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Guard QR-AD-033: OpenAPI (<c>/openapi/v1.json</c>) CHỈ phơi ở Development, TẮT ở Production (posture nội-mạng —
/// không phơi bề mặt API ra ngoài; opt-in DV-015). Boot Host thật qua <see cref="WebApplicationFactory{TEntryPoint}"/>
/// với ĐÚNG environment + secret nạp ngoài repo (F35). Chứng minh env-gating THẬT: đổi môi trường = đổi hành vi.
/// </summary>
public sealed class OpenApiExposureTests
{
    private static WebApplicationFactory<Program> Factory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Secret + connection string cấp lúc test (như dev nạp User-Secrets) — smoke không chạm DB.
                ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
                ["ConnectionStrings:Identity"] = "Host=localhost;Port=5432;Database=t;Username=postgres;Password=test",
                ["ConnectionStrings:ResortConfig"] = "Host=localhost;Port=5432;Database=t;Username=postgres;Password=test",
                ["ConnectionStrings:Rooms"] = "Host=localhost;Port=5432;Database=t;Username=postgres;Password=test",
                ["ConnectionStrings:GuestAccess"] = "Host=localhost;Port=5432;Database=t;Username=postgres;Password=test",
                ["Bedrock:Messaging:AllowOutboxWithoutDispatcher"] = "true",
            }));
        });

    private static Uri OpenApiDoc => new("/openapi/v1.json", UriKind.Relative);

    [Fact]
    public async Task OpenApi_document_is_served_in_development()
    {
        using var factory = Factory("Development");
        var client = factory.CreateClient();

        var response = await client.GetAsync(OpenApiDoc);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase); // là tài liệu OpenAPI hợp lệ.
    }

    [Fact]
    public async Task OpenApi_document_is_absent_in_production()
    {
        using var factory = Factory("Production");
        var client = factory.CreateClient();

        var response = await client.GetAsync(OpenApiDoc);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // Prod KHÔNG phơi OpenAPI.
    }
}
