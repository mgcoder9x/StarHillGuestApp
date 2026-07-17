using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Api.Authentication;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Time;
using Concierge.Contracts;
using Housekeeping.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;
using Rules.Contracts;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// Guard endpoint Dashboard tổng hợp Ở HOST (task 11.1, Req 9.1): <c>GET /v1/dashboard/stats</c> = RequireStaff
/// (no-token→401; Staff/Admin→200 superset), aggregate map ĐÚNG 5 số từ 4 query-port module, resortId-null→
/// configuration_unavailable (problem+json, KHÔNG 200), và "hôm nay" = đầu ngày UTC truyền cho Rules. TestServer map
/// DashboardEndpointModule THẬT + fake query (KHÔNG DB/Docker).
/// </summary>
public sealed class DashboardEndpointTests
{
    private sealed class FixedClock : IClock
    {
        // 2026-07-17 09:30 UTC → đầu ngày = 2026-07-17 00:00 UTC.
        public DateTimeOffset UtcNow { get; } = new(2026, 7, 17, 9, 30, 0, TimeSpan.Zero);
    }

    private sealed class FakeConciergeStats : IConciergeStatsQuery
    {
        public Task<ConciergeStats> GetStatsAsync(Guid resortId, CancellationToken ct = default) =>
            Task.FromResult(new ConciergeStats(OpenConversations: 3, UnreadConversations: 5));
    }

    private sealed class FakeHousekeepingStats : IHousekeepingStatsQuery
    {
        public Task<int> CountOpenTicketsAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(7);
    }

    private sealed class FakeRoomStats : IRoomStatsQuery
    {
        public Task<int> CountActiveRoomsAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(42);
    }

    private sealed class FakeRulesStats : IRulesStatsQuery
    {
        public DateTimeOffset? SinceSeen { get; private set; }

        public Task<int> CountAcknowledgementsSinceAsync(Guid resortId, DateTimeOffset since, CancellationToken ct = default)
        {
            SinceSeen = since;
            return Task.FromResult(11);
        }
    }

    private sealed class NullSettingsQuery : IResortSettingsQuery
    {
        public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) =>
            Task.FromResult<ResortSettingsSnapshot?>(null);
    }

    private static async Task<IHost> StartAsync(bool settingsNull = false, FakeRulesStats? rulesStats = null)
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

                services.AddSingleton<IClock, FixedClock>();
                if (settingsNull)
                {
                    services.AddScoped<IResortSettingsQuery, NullSettingsQuery>();
                }
                else
                {
                    services.AddScoped<IResortSettingsQuery, FakeResortSettingsQuery>();
                }

                services.AddScoped<IConciergeStatsQuery, FakeConciergeStats>();
                services.AddScoped<IHousekeepingStatsQuery, FakeHousekeepingStats>();
                services.AddScoped<IRoomStatsQuery, FakeRoomStats>();
                services.AddSingleton<IRulesStatsQuery>(rulesStats ?? new FakeRulesStats());
            });
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => new DashboardEndpointModule().MapEndpoints(endpoints));
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
    public async Task Missing_token_unauthorized()
    {
        using var host = await StartAsync();
        var client = Client(host, role: null);

        var response = await client.GetAsync(Rel("/v1/dashboard/stats"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_gets_aggregated_stats()
    {
        var rules = new FakeRulesStats();
        using var host = await StartAsync(rulesStats: rules);
        var client = Client(host, StarHillPolicies.RoleStaff);

        var response = await client.GetAsync(Rel("/v1/dashboard/stats"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsResponse>();
        Assert.NotNull(stats);
        Assert.Equal(5, stats!.UnreadConversations);
        Assert.Equal(3, stats.OpenConversations);
        Assert.Equal(7, stats.OpenHousekeepingTickets);
        Assert.Equal(42, stats.ActiveRooms);
        Assert.Equal(11, stats.RulesAcksToday);

        // "Hôm nay" = đầu ngày UTC (2026-07-17 00:00 UTC) — Host tính, truyền since cho Rules.
        Assert.Equal(new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero), rules.SinceSeen);
    }

    [Fact]
    public async Task Admin_gets_stats_superset()
    {
        using var host = await StartAsync();
        var client = Client(host, StarHillPolicies.RoleAdmin);

        var response = await client.GetAsync(Rel("/v1/dashboard/stats"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Configuration_unavailable_when_settings_missing()
    {
        using var host = await StartAsync(settingsNull: true);
        var client = Client(host, StarHillPolicies.RoleStaff);

        var response = await client.GetAsync(Rel("/v1/dashboard/stats"));
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
