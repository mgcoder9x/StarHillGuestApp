using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace ResortQr.IntegrationTests.Api;

/// <summary>
/// HTTP end-to-end admin rooms (SQLite-backed AppWebFactory): phân quyền (401/403), happy-path 201,
/// validation. Bao Task 3.3 (Staff bị chặn endpoint Admin) + Task 4 endpoint.
/// </summary>
public sealed class AdminRoomEndpointsTests
{
    private sealed record CreateRoomResponse(Guid RoomId, string Token, string TokenPreview);

    [Fact]
    public async Task Post_room_without_token_returns_401()
    {
        using var factory = new AppWebFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "101" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_rooms_without_token_returns_401()
    {
        using var factory = new AppWebFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/rooms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_create_room_returns_403()
    {
        using var factory = new AppWebFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AppWebFactory.StaffEmail, AppWebFactory.StaffPassword);

        var response = await client.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "101" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Staff_can_list_rooms_returns_200()
    {
        using var factory = new AppWebFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AppWebFactory.StaffEmail, AppWebFactory.StaffPassword);

        var response = await client.GetAsync("/api/admin/rooms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_creates_room_then_lists_and_renders_qr()
    {
        using var factory = new AppWebFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AppWebFactory.AdminEmail, AppWebFactory.AdminPassword);

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "101", building = "A", floor = 3 });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.RoomId);

        // List shows it
        var listResponse = await client.GetAsync("/api/admin/rooms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("101", listJson, StringComparison.Ordinal);

        // qr.png (settings seeded with https base url) → image/png
        var qrResponse = await client.GetAsync($"/api/admin/rooms/{created.RoomId}/qr.png");
        Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);
        Assert.Equal("image/png", qrResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await qrResponse.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
    }

    [Fact]
    public async Task Admin_create_with_empty_room_number_returns_400()
    {
        using var factory = new AppWebFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AppWebFactory.AdminEmail, AppWebFactory.AdminPassword);

        var response = await client.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_revoke_token_returns_new_token()
    {
        using var factory = new AppWebFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AppWebFactory.AdminEmail, AppWebFactory.AdminPassword);

        var created = await (await client.PostAsJsonAsync("/api/admin/rooms", new { roomNumber = "202" }))
            .Content.ReadFromJsonAsync<CreateRoomResponse>();

        var rotateResponse = await client.PostAsJsonAsync($"/api/admin/rooms/{created!.RoomId}/revoke-token", new { reason = "reprint" });

        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<CreateRoomResponse>();
        Assert.NotEqual(created.Token, rotated!.Token);
    }
}
