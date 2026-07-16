using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bedrock.Api.Authentication;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using GuestAccess.Contracts;
using Housekeeping.Api;
using Housekeeping.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard ánh xạ role endpoint module Housekeeping (H-Hk.3): admin <c>/v1/housekeeping/*</c> = RequireStaff (Staff→2xx;
/// no-token→401); guest <c>/v1/guest/housekeeping</c> = AllowAnonymous (resolve context; resolver-fail→ProblemDetails,
/// KHÔNG 401). TestServer map module THẬT + fake use case/reader (KHÔNG DB).
/// </summary>
public sealed class HousekeepingEndpointAuthTests
{
    private static readonly Guid TicketId = FakeRequestHousekeeping.TicketId;

    private static async Task<IHost> StartAsync(FakeCurrentGuestContextResolver resolver)
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer();
            webHost.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddBedrockApiVersioning();
                services.AddBedrockAuthCore(JwtTestTokens.BuildConfig());
                services.AddStarHillAuthorization();
                // Khớp Host (QR-AD-021): JSON string-enum để body {status:"InProgress"} bind HousekeepingStatus.
                services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddSingleton<ICurrentGuestContextResolver>(resolver);
                services.AddScoped<IHousekeepingReader, FakeHousekeepingReader>();

                services.AddScoped<IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>, FakeRequestHousekeeping>();
                services.AddScoped<IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult>, FakeGetRoomHousekeepingStatus>();
                services.AddScoped<IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>, FakeSetHousekeepingStatus>();
                services.AddScoped<IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>, FakeCompleteHousekeepingByRoom>();
                services.AddScoped<IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>, FakeCompleteHousekeepingByToken>();
                services.AddScoped<IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult>, FakeCreateHousekeepingByStaff>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    new HousekeepingAdminEndpointModule().MapEndpoints(endpoints);
                    new HousekeepingGuestEndpointModule().MapEndpoints(endpoints);
                });
            });
        });

        return await builder.StartAsync();
    }

    private static HttpClient Client(IHost host, string? role)
    {
        var client = host.GetTestClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue(role));
        }

        return client;
    }

    private static FakeCurrentGuestContextResolver OkResolver() => new()
    {
        NextResult = Result.Success(new CurrentGuestContext(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), FakeResortSettingsQuery.ResortId)),
    };

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Staff_can_operate_housekeeping()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel("/v1/housekeeping"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/housekeeping"), JsonContent.Create(new { roomId = Guid.CreateVersion7() }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel($"/v1/housekeeping/{TicketId}/status"), JsonContent.Create(new { status = "InProgress" }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/housekeeping/complete-by-room"), JsonContent.Create(new { roomId = Guid.CreateVersion7() }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/housekeeping/complete-by-token"), JsonContent.Create(new { token = "tok-123" }))).StatusCode);
    }

    [Fact]
    public async Task Missing_token_unauthorized_on_admin()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Rel("/v1/housekeeping"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/housekeeping/complete-by-room"), JsonContent.Create(new { roomId = Guid.CreateVersion7() }))).StatusCode);
    }

    [Fact]
    public async Task Guest_can_request_and_view_without_token()
    {
        using var host = await StartAsync(OkResolver());
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel("/v1/guest/housekeeping"), JsonContent.Create(new { roomId = Guid.CreateVersion7() }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync(Rel($"/v1/guest/housekeeping?roomId={Guid.CreateVersion7()}"))).StatusCode);
    }

    [Fact]
    public async Task Guest_resolver_failure_returns_problem_not_unauthorized()
    {
        var resolver = new FakeCurrentGuestContextResolver
        {
            NextResult = Result.Failure<CurrentGuestContext>(Error.Validation("guest_context_missing", "no cookie")),
        };
        using var host = await StartAsync(resolver);
        var client = Client(host, role: null);

        var response = await client.PostAsync(Rel("/v1/guest/housekeeping"), JsonContent.Create(new { roomId = Guid.CreateVersion7() }));
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
