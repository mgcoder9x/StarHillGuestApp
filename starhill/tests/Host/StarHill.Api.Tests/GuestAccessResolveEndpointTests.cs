using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Api;
using GuestAccess.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace StarHill.Api.Tests;

/// <summary>
/// Guard endpoint guest công khai <c>POST /v1/guest/resolve</c> (C-GA.3): AllowAnonymous (Req 11.2); cookie thiết
/// bị <c>__Host-</c> HttpOnly/Secure/SameSite=Lax/Path=/ CHỈ khi phát session mới (QR-AD-025); <c>Cache-Control:
/// no-store</c>; NON-DISCLOSURE (raw session key CHỈ ở Set-Cookie, KHÔNG trong body); token trong BODY (QR-DV-006);
/// lỗi → KHÔNG set cookie. Dựng TestServer map <see cref="GuestAccessEndpointModule"/> THẬT + fake use case (không DB).
/// </summary>
public sealed class GuestAccessResolveEndpointTests
{
    private const string IssuedKey = "raw-secret-session-key-DO-NOT-LEAK";

    private sealed class FakeResolve(Result<ResolveTokenResult> result) : IUseCase<ResolveTokenInput, ResolveTokenResult>
    {
        public ResolveTokenInput? LastInput { get; private set; }

        public Task<Result<ResolveTokenResult>> ExecuteAsync(ResolveTokenInput input, CancellationToken ct = default)
        {
            LastInput = input;
            return Task.FromResult(result);
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private static ResolveTokenResult SampleResult(string? issuedKey) => new(
        RoomId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
        RoomNumber: "A-101",
        Building: "A",
        Floor: 1,
        ResortId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
        ResortName: "Star Hill",
        LogoUrl: null,
        VisitId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
        PortalWindowExpiresAt: new DateTimeOffset(2026, 1, 1, 12, 30, 0, TimeSpan.Zero),
        Languages: ["en", "vi"],
        DefaultLanguageCode: "en",
        FaqEnabled: true,
        ChatEnabled: true,
        HousekeepingEnabled: true,
        RequireRuleAckForFaq: true,
        RequireRuleAckForChat: true,
        RequireRuleAckForHousekeeping: true,
        IssuedSessionKey: issuedKey);

    private static async Task<IHost> StartAsync(Result<ResolveTokenResult> result)
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockApiVersioning();
                services.AddSingleton<IClock, FixedClock>();
                services.Configure<GuestAccessOptions>(_ => { }); // mặc định: __Host-starhill_guest, 60 ngày.
                services.AddSingleton<IUseCase<ResolveTokenInput, ResolveTokenResult>>(new FakeResolve(result));
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => new GuestAccessEndpointModule().MapEndpoints(endpoints));
            });
        });

        return await builder.StartAsync();
    }

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Resolve_is_anonymous_and_sets_host_cookie_on_new_session()
    {
        using var host = await StartAsync(Result.Success(SampleResult(IssuedKey)));
        var client = host.GetTestClient(); // KHÔNG Authorization header → chứng minh AllowAnonymous.

        var response = await client.PostAsync(Rel("/v1/guest/resolve"), JsonContent.Create(new { token = "qr-token" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cache-Control: no-store.
        Assert.True(response.Headers.CacheControl?.NoStore);

        // Set-Cookie __Host- với đủ thuộc tính bảo mật.
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var setCookie = cookies!.Single();
        Assert.Contains("__Host-starhill_guest=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", setCookie, StringComparison.OrdinalIgnoreCase);

        // NON-DISCLOSURE: raw session key CHỈ ở cookie, KHÔNG trong body JSON.
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(IssuedKey, setCookie, StringComparison.Ordinal);
        Assert.DoesNotContain(IssuedKey, body, StringComparison.Ordinal);
        // Body vẫn mang ngữ cảnh phòng/visit.
        Assert.Contains("A-101", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolve_does_not_set_cookie_when_session_reused()
    {
        using var host = await StartAsync(Result.Success(SampleResult(issuedKey: null)));
        var client = host.GetTestClient();

        var response = await client.PostAsync(Rel("/v1/guest/resolve"), JsonContent.Create(new { token = "qr-token" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie")); // nối lại session cũ → KHÔNG cấp cookie mới.
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Resolve_failure_returns_problem_without_cookie()
    {
        using var host = await StartAsync(Result.Failure<ResolveTokenResult>(GuestAccessErrors.QrInvalid));
        var client = host.GetTestClient();

        var response = await client.PostAsync(Rel("/v1/guest/resolve"), JsonContent.Create(new { token = "bad" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); // qr_invalid → NotFound(404).
        Assert.False(response.Headers.Contains("Set-Cookie")); // lỗi → KHÔNG set cookie.
        Assert.True(response.Headers.CacheControl?.NoStore);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("qr_invalid", body, StringComparison.Ordinal); // ProblemDetails mang code ổn định.
    }

    [Fact]
    public async Task Resolve_rejects_get_method()
    {
        using var host = await StartAsync(Result.Success(SampleResult(IssuedKey)));
        var client = host.GetTestClient();

        var response = await client.GetAsync(Rel("/v1/guest/resolve"));

        // Chỉ map POST → GET không khớp (405 Method Not Allowed hoặc 404).
        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"Expected 405/404 for GET, got {(int)response.StatusCode}.");
    }

    // QR-AD-025 (C-GA.3a): response guest là contract NESTED (room/resort/languages/defaultLanguage/visit/features).
    // Snapshot khoá shape để đổi cấu trúc = vỡ test (chống drift âm thầm giữa design §7 và code).
    [Fact]
    public async Task Resolve_success_body_matches_nested_contract()
    {
        using var host = await StartAsync(Result.Success(SampleResult(IssuedKey)));
        var client = host.GetTestClient();

        var response = await client.PostAsync(Rel("/v1/guest/resolve"), JsonContent.Create(new { token = "qr-token" }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var room = root.GetProperty("room");
        Assert.Equal("A-101", room.GetProperty("number").GetString());
        Assert.Equal("A", room.GetProperty("building").GetString());
        Assert.Equal(1, room.GetProperty("floor").GetInt32());

        var resort = root.GetProperty("resort");
        Assert.Equal("Star Hill", resort.GetProperty("name").GetString());

        Assert.Equal("en", root.GetProperty("defaultLanguage").GetString());
        Assert.Equal(["en", "vi"], root.GetProperty("languages").EnumerateArray().Select(e => e.GetString()!).ToArray());

        var visit = root.GetProperty("visit");
        Assert.Equal("77777777-7777-7777-7777-777777777777", visit.GetProperty("id").GetString());
        Assert.True(visit.TryGetProperty("portalWindowExpiresAt", out _));

        var features = root.GetProperty("features");
        Assert.True(features.GetProperty("faqEnabled").GetBoolean());
        Assert.True(features.GetProperty("ruleAckRequiredForFaq").GetBoolean());

        // NON-DISCLOSURE (kép): không có field session/hash/token trong body dưới bất kỳ tên nào.
        var raw = root.GetRawText();
        Assert.DoesNotContain("sessionKey", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", raw, StringComparison.OrdinalIgnoreCase);
    }

    // QR-AD-025 (C-GA.3a): endpoint khai request-size limit 1 KiB (contract). Kestrel enforce qua
    // IHttpMaxRequestBodySizeFeature ở production; guard này khoá GIÁ TRỊ khai báo để không bị gỡ âm thầm.
    [Fact]
    public async Task Resolve_declares_request_body_size_limit()
    {
        using var host = await StartAsync(Result.Success(SampleResult(IssuedKey)));
        var dataSource = host.Services.GetRequiredService<EndpointDataSource>();

        var endpoint = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText is not null
                && e.RoutePattern.RawText.Contains("guest/resolve", StringComparison.Ordinal));

        var limit = endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>();
        Assert.NotNull(limit);
        Assert.Equal(1024, limit!.MaxRequestBodySize);
    }
}
