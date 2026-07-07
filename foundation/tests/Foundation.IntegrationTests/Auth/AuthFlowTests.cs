using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Foundation.IntegrationTests.Auth;

public sealed class AuthFlowTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthFlowTests(AuthApiFactory factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private sealed record TokenDto(string AccessToken, DateTimeOffset ExpiresAt);

    private static string? ExtractRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var raw = cookies.FirstOrDefault(c => c.StartsWith("refresh_token=", StringComparison.Ordinal));
        if (raw is null)
        {
            return null;
        }

        var firstSegment = raw.Split(';')[0];
        return firstSegment["refresh_token=".Length..];
    }

    [Fact]
    public async Task Login_valid_should_return_token_and_me_should_work()
    {
        var userId = _factory.SeedUser("admin@it.com", "pw12345678");
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "admin@it.com", password = "pw12345678" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var token = await login.Content.ReadFromJsonAsync<TokenDto>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
        meRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        var me = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(userId.ToString(), doc.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Login_wrong_password_should_return_401_problem()
    {
        _factory.SeedUser("wrong@it.com", "correct-password");
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "wrong@it.com", password = "bad-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("application/problem+json", login.Content.Headers.ContentType!.MediaType);

        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.Equal("invalid_credentials", doc.RootElement.GetProperty("code").GetString());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Me_without_token_should_return_401()
    {
        var client = CreateClient();

        var me = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        // 401 phải trả ProblemDetails đồng nhất (code + traceId), không phải response mặc định rỗng (fix expert #3).
        Assert.Equal("application/problem+json", me.Content.Headers.ContentType!.MediaType);
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("unauthorized", doc.RootElement.GetProperty("code").GetString());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Refresh_should_rotate_and_return_new_access_token()
    {
        _factory.SeedUser("refresh@it.com", "pw12345678");
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "refresh@it.com", password = "pw12345678" });
        var refreshCookie = ExtractRefreshCookie(login);
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refresh_token={refreshCookie}");
        var refresh = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotatedCookie = ExtractRefreshCookie(refresh);
        Assert.False(string.IsNullOrWhiteSpace(rotatedCookie));
        Assert.NotEqual(refreshCookie, rotatedCookie);
    }

    [Fact]
    public async Task Reused_refresh_cookie_should_be_rejected()
    {
        _factory.SeedUser("reuse@it.com", "pw12345678");
        var client = CreateClient();

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "reuse@it.com", password = "pw12345678" });
        var cookie = ExtractRefreshCookie(login);

        using var first = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        first.Headers.Add("Cookie", $"refresh_token={cookie}");
        var firstRefresh = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        // Trình lại cookie CŨ (đã xoay) → reuse detection → 401.
        using var reuse = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        reuse.Headers.Add("Cookie", $"refresh_token={cookie}");
        var reuseRefresh = await client.SendAsync(reuse);

        Assert.Equal(HttpStatusCode.Unauthorized, reuseRefresh.StatusCode);
    }
}
