using System.Net;
using System.Net.Http.Headers;
using Bedrock.Api.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Bedrock.Api.Tests.Authentication;

public sealed class AuthMechanismTests
{
    private const string Kid = "k1";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private static readonly byte[] SecretBytes = new byte[32]; // 256-bit (đủ HS256, test).
    private static readonly string SecretBase64 = Convert.ToBase64String(SecretBytes);

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ActiveKid"] = Kid,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Keys:0:Kid"] = Kid,
                ["Jwt:Keys:0:Secret"] = SecretBase64,
            })
            .Build();

    private static async Task<IHost> StartHostAsync()
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockAuthCore(BuildConfig());
                services.AddAuthorizationBuilder().AddPolicy("admin-only", p => p.RequireRole("admin"));
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/protected", () => Results.Ok("ok")).RequireAuthorization();
                    endpoints.MapGet("/admin", () => Results.Ok("ok")).RequireAuthorization("admin-only");
                });
            });
        });

        return await builder.StartAsync();
    }

    private static string IssueTokenWithoutRole()
    {
        var key = new SymmetricSecurityKey(SecretBytes) { KeyId = Kid };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["sub"] = Guid.CreateVersion7().ToString() },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task Missing_token_should_return_401_problem_details()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync(new Uri("/protected", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("unauthorized", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticated_but_missing_role_should_return_403_problem_details()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenWithoutRole());

        var response = await client.GetAsync(new Uri("/admin", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("forbidden", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_token_should_authorize_protected_endpoint()
    {
        using var host = await StartHostAsync();
        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenWithoutRole());

        var response = await client.GetAsync(new Uri("/protected", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Verify_only_host_with_empty_key_ring_fails_during_startup()
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
                services.AddBedrockAuthCore(new ConfigurationBuilder().Build()));
            webHost.Configure(_ => { });
        });

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => builder.StartAsync());

        Assert.Contains("Keys rỗng", error.Message, StringComparison.Ordinal);
    }
}
