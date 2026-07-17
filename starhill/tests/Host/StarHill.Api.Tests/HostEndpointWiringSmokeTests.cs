using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// SMOKE WIRING endpoint ở HOST THẬT đã compose (WebApplicationFactory) — KHÔNG DB. Phủ đúng lớp lỗi mà "mở web
/// bằng browser" hay lộ nhưng test module lẻ KHÔNG bắt: mỗi module lẻ tự map endpoint trong TestServer, KHÔNG chứng
/// minh <c>AddRoomsApi/AddResortConfigApi/AddRulesApi/AddGuestAccessApi</c> THẬT SỰ được Host wire. Ở đây:
/// <list type="bullet">
/// <item>Endpoint admin protected KHÔNG token → <b>401</b> (nếu quên wire → 404; nếu quên bảo vệ → 200/khác) — bắt cả hai lỗi.</item>
/// <item>Endpoint guest AllowAnonymous KHÔNG cookie → resolver short-circuit <c>guest_context_missing</c> (KHÔNG DB)
/// → <c>application/problem+json</c> (chứng minh endpoint mapped + đi qua pipeline; routing-miss KHÔNG có body này).</item>
/// <item>Route lạ → 404.</item>
/// </list>
/// Auth 401 xảy ra ở middleware TRƯỚC handler → KHÔNG chạm DB. Chạy mọi máy (không cần Docker).
/// </summary>
public sealed class HostEndpointWiringSmokeTests : IClassFixture<SecretInjectingHostFactory>
{
    private readonly SecretInjectingHostFactory _factory;

    public HostEndpointWiringSmokeTests(SecretInjectingHostFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    // Endpoint admin protected (RequireStaff/RequireAdmin) — KHÔNG token phải 401 (đã wire + đã bảo vệ).
    [Theory]
    [InlineData("GET", "/v1/rooms")]
    [InlineData("POST", "/v1/rooms")]
    [InlineData("GET", "/v1/rooms/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/v1/rooms/11111111-1111-1111-1111-111111111111/qr.png")]
    [InlineData("GET", "/v1/resort/settings")]
    [InlineData("PUT", "/v1/resort/settings")]
    [InlineData("POST", "/v1/rules/sections")]
    [InlineData("PUT", "/v1/rules/sections/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/v1/rules/publish")]
    [InlineData("GET", "/v1/rules/preview")]
    [InlineData("GET", "/v1/rules/publications")]
    [InlineData("POST", "/v1/faq/categories")]
    [InlineData("POST", "/v1/faq/items")]
    [InlineData("POST", "/v1/faq/reorder/categories")]
    [InlineData("GET", "/v1/housekeeping")]
    [InlineData("POST", "/v1/housekeeping/complete-by-room")]
    [InlineData("POST", "/v1/housekeeping/complete-by-token")]
    [InlineData("GET", "/v1/conversations")]
    [InlineData("GET", "/v1/conversations/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/v1/conversations/11111111-1111-1111-1111-111111111111/reply")]
    [InlineData("POST", "/v1/conversations/11111111-1111-1111-1111-111111111111/read")]
    [InlineData("POST", "/v1/conversations/11111111-1111-1111-1111-111111111111/close")]
    [InlineData("POST", "/v1/notes")]
    [InlineData("PUT", "/v1/notes/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/v1/notes/11111111-1111-1111-1111-111111111111")]
    public async Task Protected_admin_endpoint_requires_authentication(string method, string path)
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/v1/guest/rules?roomId=11111111-1111-1111-1111-111111111111")]
    [InlineData("/v1/guest/faq?roomId=11111111-1111-1111-1111-111111111111")]
    [InlineData("/v1/guest/housekeeping?roomId=11111111-1111-1111-1111-111111111111")]
    [InlineData("/v1/guest/conversation?roomId=11111111-1111-1111-1111-111111111111")]
    public async Task Guest_get_rules_is_mapped_and_returns_problem_without_cookie(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        // Endpoint mapped + đi qua ProblemDetails (resolver short-circuit guest_context_missing — KHÔNG DB).
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_route_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/v1/no-such-endpoint", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
