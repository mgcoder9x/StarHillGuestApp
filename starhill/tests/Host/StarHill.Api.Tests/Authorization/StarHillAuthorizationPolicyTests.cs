using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Bedrock.Api.Authentication;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard CP8 (QR-AD-005/QR-AD-020) — ngữ nghĩa policy sản phẩm ĐỘC LẬP endpoint nghiệp vụ: chứng minh
/// <b>Admin ⊇ Staff</b> ở tầng policy. Dựng TestServer tối thiểu (KHÔNG DB/Docker) với hai stub endpoint gắn
/// <see cref="StarHillPolicies.RequireAdmin"/> / <see cref="StarHillPolicies.RequireStaff"/>.
/// </summary>
public sealed class StarHillAuthorizationPolicyTests
{
    private static async Task<IHost> StartAsync()
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockAuthCore(JwtTestTokens.BuildConfig());
                services.AddStarHillAuthorization();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/admin-only", () => Results.Ok("ok"))
                        .RequireAuthorization(StarHillPolicies.RequireAdmin);
                    endpoints.MapGet("/staff-area", () => Results.Ok("ok"))
                        .RequireAuthorization(StarHillPolicies.RequireStaff);
                });
            });
        });

        return await builder.StartAsync();
    }

    private static HttpClient WithToken(IHost host, string? role)
    {
        var client = host.GetTestClient();
        // Luôn phát token (role null = token hợp lệ nhưng KHÔNG có role → kiểm 403, khác hẳn thiếu token = 401).
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(role));
        return client;
    }

    private static async Task<HttpStatusCode> GetAsync(HttpClient client, string path) =>
        (await client.GetAsync(new Uri(path, UriKind.Relative))).StatusCode;

    [Fact]
    public async Task Admin_passes_admin_only()
    {
        using var host = await StartAsync();
        var client = WithToken(host, StarHillPolicies.RoleAdmin);
        Assert.Equal(HttpStatusCode.OK, await GetAsync(client, "/admin-only"));
    }

    [Fact]
    public async Task Admin_passes_staff_area_superset()
    {
        using var host = await StartAsync();
        var client = WithToken(host, StarHillPolicies.RoleAdmin);
        Assert.Equal(HttpStatusCode.OK, await GetAsync(client, "/staff-area"));
    }

    [Fact]
    public async Task Staff_passes_staff_area()
    {
        using var host = await StartAsync();
        var client = WithToken(host, StarHillPolicies.RoleStaff);
        Assert.Equal(HttpStatusCode.OK, await GetAsync(client, "/staff-area"));
    }

    [Fact]
    public async Task Staff_forbidden_on_admin_only()
    {
        using var host = await StartAsync();
        var client = WithToken(host, StarHillPolicies.RoleStaff);
        Assert.Equal(HttpStatusCode.Forbidden, await GetAsync(client, "/admin-only"));
    }

    [Fact]
    public async Task Authenticated_without_role_forbidden_on_both()
    {
        using var host = await StartAsync();
        var client = WithToken(host, role: null);
        Assert.Equal(HttpStatusCode.Forbidden, await GetAsync(client, "/admin-only"));
        Assert.Equal(HttpStatusCode.Forbidden, await GetAsync(client, "/staff-area"));
    }

    [Fact]
    public async Task Missing_token_unauthorized_on_both()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.Unauthorized, await GetAsync(client, "/admin-only"));
        Assert.Equal(HttpStatusCode.Unauthorized, await GetAsync(client, "/staff-area"));
    }
}
