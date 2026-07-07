using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ResortQr.IntegrationTests.Api;

/// <summary>
/// HTTP end-to-end guest resolve (SQLite-backed AppWebFactory): thiết bị mới → 200 + Set-Cookie;
/// token sai → 404 problem+json (không lộ phòng); nối lại visit khi trình lại cookie.
/// LƯU Ý: cookie Secure KHÔNG tự round-trip qua http test server → trích Set-Cookie + gửi tay (như AuthFlowTests).
/// </summary>
public sealed class GuestResolveEndpointTests
{
    private sealed record CreateRoomResponse(Guid RoomId, string Token, string TokenPreview);
    private sealed record ResolveBody(Guid RoomId, string RoomNumber, Guid VisitId, string? DefaultLanguageCode);

    private static HttpClient NoCookieClient(AppWebFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static async Task<string> CreateRoomTokenAsync(AppWebFactory factory)
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(AppWebFactory.AdminEmail, AppWebFactory.AdminPassword);
        var created = await (await admin.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "101" }))
            .Content.ReadFromJsonAsync<CreateRoomResponse>();
        return created!.Token;
    }

    private static string? ExtractGuestCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var raw = cookies.FirstOrDefault(c => c.StartsWith("shq_guest=", StringComparison.Ordinal));
        return raw?.Split(';')[0]["shq_guest=".Length..];
    }

    [Fact]
    public async Task Resolve_new_device_returns_200_and_sets_guest_cookie()
    {
        using var factory = new AppWebFactory();
        var token = await CreateRoomTokenAsync(factory);

        using var client = NoCookieClient(factory);
        var response = await client.GetAsync($"/api/guest/resolve/{token}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("shq_guest=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadFromJsonAsync<ResolveBody>();
        Assert.NotNull(body);
        Assert.Equal("101", body!.RoomNumber);
        Assert.NotEqual(Guid.Empty, body.VisitId);
        Assert.Equal("en", body.DefaultLanguageCode);

        // Body KHÔNG chứa session key raw.
        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("issuedSessionKey", rawBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_unknown_token_returns_404_problem_json()
    {
        using var factory = new AppWebFactory();
        using var client = NoCookieClient(factory);

        var response = await client.GetAsync("/api/guest/resolve/nonexistent-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("qr_invalid", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolve_twice_with_returned_cookie_resumes_same_visit()
    {
        using var factory = new AppWebFactory();
        var token = await CreateRoomTokenAsync(factory);
        using var client = NoCookieClient(factory);

        var first = await client.GetAsync($"/api/guest/resolve/{token}");
        var firstBody = await first.Content.ReadFromJsonAsync<ResolveBody>();
        var cookie = ExtractGuestCookie(first);
        Assert.False(string.IsNullOrEmpty(cookie));

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/guest/resolve/{token}");
        secondRequest.Headers.Add("Cookie", $"shq_guest={cookie}");
        var second = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<ResolveBody>();
        Assert.Equal(firstBody!.VisitId, secondBody!.VisitId); // nối lại đúng visit
        Assert.False(second.Headers.Contains("Set-Cookie"));   // đã có session → không set cookie mới
    }
}
