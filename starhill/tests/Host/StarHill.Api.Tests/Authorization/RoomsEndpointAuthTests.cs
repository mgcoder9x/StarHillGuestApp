using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bedrock.Api.Authentication;
using Bedrock.Api.Versioning;
using Bedrock.Application.UseCases;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using Rooms.Api;
using Rooms.Application;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard ánh xạ role TỪNG endpoint Rooms (Req 7.6, QR-AD-019) + route shape <c>/v1/rooms</c> (QR-DV-001) +
/// content-type <c>image/png</c> của qr.png. Dựng TestServer map <see cref="RoomsEndpointModule"/> THẬT +
/// fake use case (KHÔNG DB/Docker). Chứng minh: Staff bị 403 mọi endpoint Admin; Admin qua được; qr.png Staff+Admin OK.
/// </summary>
public sealed class RoomsEndpointAuthTests
{
    private static readonly Guid RoomId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static async Task<IHost> StartAsync()
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
                // Mirror chính sách JSON của Host: enum nhận/trả dạng string (RoomStatus "Active"/"Inactive"/...).
                services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

                services.AddScoped<IUseCase<CreateRoomInput, CreateRoomResult>, FakeCreateRoom>();
                services.AddScoped<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>, FakeRotateToken>();
                services.AddScoped<IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>, FakeRenderQrPng>();
                services.AddScoped<ICommandUseCase<UpdateRoomInput>, FakeUpdateRoom>();
                services.AddScoped<ICommandUseCase<ChangeRoomStatusInput>, FakeChangeStatus>();
                services.AddScoped<ICommandUseCase<Guid>, FakeDeleteRoom>();
                services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                services.AddScoped<IRoomQueries, FakeRoomQueries>();
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => new RoomsEndpointModule().MapEndpoints(endpoints));
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

    private static Uri Rel(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Staff_forbidden_on_all_admin_endpoints()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleStaff);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsync(Rel("/v1/rooms"), JsonContent.Create(new { roomNumber = "101" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsync(Rel($"/v1/rooms/{RoomId}"), JsonContent.Create(new { roomNumber = "102" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsync(Rel($"/v1/rooms/{RoomId}/status"), JsonContent.Create(new { status = "Inactive" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.DeleteAsync(Rel($"/v1/rooms/{RoomId}"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsync(Rel($"/v1/rooms/{RoomId}/rotate-token"), JsonContent.Create(new { reason = "x" }))).StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_room_returns_201_with_v1_location()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleAdmin);

        var response = await client.PostAsync(Rel("/v1/rooms"), JsonContent.Create(new { roomNumber = "101" }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/v1/rooms/", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_mutations_succeed()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleAdmin);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsync(Rel($"/v1/rooms/{RoomId}"), JsonContent.Create(new { roomNumber = "102" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PatchAsync(Rel($"/v1/rooms/{RoomId}/status"), JsonContent.Create(new { status = "Inactive" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync(Rel($"/v1/rooms/{RoomId}"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync(Rel($"/v1/rooms/{RoomId}/rotate-token"), JsonContent.Create(new { reason = "x" }))).StatusCode);
    }

    [Theory]
    [InlineData(StarHillPolicies.RoleStaff)]
    [InlineData(StarHillPolicies.RoleAdmin)]
    public async Task QrPng_viewable_by_staff_and_admin_returns_png(string role)
    {
        using var host = await StartAsync();
        var client = Client(host, role);

        var response = await client.GetAsync(Rel($"/v1/rooms/{RoomId}/qr.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Missing_token_unauthorized()
    {
        using var host = await StartAsync();
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsync(Rel("/v1/rooms"), JsonContent.Create(new { roomNumber = "101" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(Rel($"/v1/rooms/{RoomId}/qr.png"))).StatusCode);
    }

    // ─────────────────────── B-Rooms.4: GET list/detail (RequireStaff) ───────────────────────
    [Theory]
    [InlineData(StarHillPolicies.RoleStaff)]
    [InlineData(StarHillPolicies.RoleAdmin)]
    public async Task List_and_detail_viewable_by_staff_and_admin(string role)
    {
        using var host = await StartAsync();
        var client = Client(host, role);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel("/v1/rooms"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Rel($"/v1/rooms/{FakeRoomQueries.KnownRoomId}"))).StatusCode);
    }

    [Fact]
    public async Task Detail_unknown_room_returns_404()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleStaff);

        var response = await client.GetAsync(Rel($"/v1/rooms/{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_requires_authentication()
    {
        using var host = await StartAsync();
        var client = Client(host, role: null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Rel("/v1/rooms"))).StatusCode);
    }
}
