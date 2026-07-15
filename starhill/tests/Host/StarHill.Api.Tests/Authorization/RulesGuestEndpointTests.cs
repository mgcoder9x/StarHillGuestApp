using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rules.Api;
using Rules.Application;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard endpoint GUEST Rules (D-Rules.4c-2, AllowAnonymous): GET đọc nội quy + POST acknowledge. TestServer +
/// fake <see cref="ICurrentGuestContextResolver"/> + fake use case (KHÔNG DB). Kiểm: context OK→200 + no-store +
/// sections; cookie được truyền đúng qua hằng <see cref="GuestAccessModule.SessionCookieName"/>; resolver Failure→
/// ProblemDetails (KHÔNG touch); ack thành công→gọi <c>TouchAsync</c> (check-before-touch — QR-AD-032).
/// </summary>
public sealed class RulesGuestEndpointTests
{
    private static readonly Guid RoomId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static CurrentGuestContext SampleContext() =>
        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            RoomId,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    private static async Task<IHost> StartAsync(FakeCurrentGuestContextResolver resolver)
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockApiVersioning();
                services.AddSingleton<ICurrentGuestContextResolver>(resolver);
                services.AddSingleton<IUseCase<GetCurrentRulesInput, GetCurrentRulesResult>>(new FakeGetCurrentRules());
                services.AddSingleton<IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult>>(new FakeAcknowledgeRules());
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => new RulesGuestEndpointModule().MapEndpoints(endpoints));
            });
        });

        return await builder.StartAsync();
    }

    private static HttpClient WithCookie(IHost host, string? sessionKey)
    {
        var client = host.GetTestClient();
        if (sessionKey is not null)
        {
            client.DefaultRequestHeaders.Add("Cookie", $"{GuestAccessModule.SessionCookieName}={sessionKey}");
        }

        return client;
    }

    [Fact]
    public async Task Get_rules_with_valid_context_returns_sections_and_no_store()
    {
        var resolver = new FakeCurrentGuestContextResolver { NextResult = Result.Success(SampleContext()) };
        using var host = await StartAsync(resolver);
        var client = WithCookie(host, "device-key-abc");

        var response = await client.GetAsync(new Uri($"/v1/guest/rules?roomId={RoomId}", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("device-key-abc", resolver.LastSessionKey); // cookie đọc qua hằng canonical.
        Assert.Contains("welcome", body, StringComparison.Ordinal);
        Assert.Equal(0, resolver.TouchCount); // GET KHÔNG touch.
    }

    [Fact]
    public async Task Get_rules_resolver_failure_returns_problem_without_touch()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Failure<CurrentGuestContext>(Error.Unauthorized("session_expired", "hết cửa sổ")),
        };
        using var host = await StartAsync(resolver);
        var client = WithCookie(host, "device-key-abc");

        var response = await client.GetAsync(new Uri($"/v1/guest/rules?roomId={RoomId}", UriKind.Relative));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, resolver.TouchCount);
    }

    [Fact]
    public async Task Acknowledge_with_valid_context_touches_window()
    {
        var resolver = new FakeCurrentGuestContextResolver { NextResult = Result.Success(SampleContext()) };
        using var host = await StartAsync(resolver);
        var client = WithCookie(host, "device-key-abc");

        var response = await client.PostAsync(
            new Uri("/v1/guest/rules/acknowledge", UriKind.Relative), JsonContent.Create(new { roomId = RoomId, lang = "en" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(1, resolver.TouchCount); // ack thành công → trượt cửa sổ SAU thành công.
    }

    [Fact]
    public async Task Acknowledge_resolver_failure_does_not_touch()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Failure<CurrentGuestContext>(Error.NotFound("guest_context_missing", "không cookie")),
        };
        using var host = await StartAsync(resolver);
        var client = WithCookie(host, sessionKey: null);

        var response = await client.PostAsync(
            new Uri("/v1/guest/rules/acknowledge", UriKind.Relative), JsonContent.Create(new { roomId = RoomId, lang = (string?)null }));

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, resolver.TouchCount);
    }
}
