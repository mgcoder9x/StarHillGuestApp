using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Bedrock.Application.Ports.Time;
using Bedrock.Domain.Results;
using Concierge.Contracts;
using Housekeeping.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;
using Rules.Contracts;
using StarHill.Authorization;

namespace StarHill.Api;

/// <summary>Số liệu tổng quan vận hành (Req 9.1) — client KHÔNG set resortId (server phân giải).</summary>
public sealed record DashboardStatsResponse(
    int UnreadConversations,
    int OpenConversations,
    int OpenHousekeepingTickets,
    int ActiveRooms,
    int RulesAcksToday);

/// <summary>
/// Endpoint Dashboard tổng hợp Ở HOST (task 11.1, Req 9.1) — Dashboard KHÔNG là module, ghép ở composition root, đọc
/// query-port stats mỗi module qua Contracts (QR-AD-002; Host chỉ chạm interface Contracts, KHÔNG chạm Infra/DbContext
/// module khác). <c>GET /v1/dashboard/stats</c> <see cref="StarHillPolicies.RequireStaff"/> (vận hành Staff+Admin).
/// resortId phân giải server qua <see cref="IResortSettingsQuery"/> (single-resort); thiếu → <c>configuration_unavailable</c>.
/// "Hôm nay" = đầu ngày UTC (Host tính qua <see cref="IClock"/>, truyền <c>since</c> cho Rules — tz resort-local là
/// refinement khi ResortSettings có field tz). Gọi TUẦN TỰ 4 query (context khác nhau, tránh dùng song song 1 context).
/// </summary>
public sealed class DashboardEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapVersionedGroup("/dashboard", BedrockApiVersioning.V1);

        group.MapGet("/stats", StatsAsync)
            .RequireAuthorization(StarHillPolicies.RequireStaff)
            .MapToApiVersion(BedrockApiVersioning.V1)
            .WithName("DashboardStats");
    }

    private static async Task<IResult> StatsAsync(
        IConciergeStatsQuery conciergeStats,
        IHousekeepingStatsQuery housekeepingStats,
        IRoomStatsQuery roomStats,
        IRulesStatsQuery rulesStats,
        IResortSettingsQuery settingsQuery,
        IClock clock,
        HttpContext http,
        CancellationToken ct)
    {
        var settings = await settingsQuery.GetAsync(ct).ConfigureAwait(false);
        if (settings is null)
        {
            return Problem(
                Error.Unexpected("configuration_unavailable", "Cấu hình resort chưa sẵn sàng."),
                http);
        }

        var resortId = settings.ResortId;
        var now = clock.UtcNow;
        var todayStartUtc = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        var concierge = await conciergeStats.GetStatsAsync(resortId, ct).ConfigureAwait(false);
        var openTickets = await housekeepingStats.CountOpenTicketsAsync(resortId, ct).ConfigureAwait(false);
        var activeRooms = await roomStats.CountActiveRoomsAsync(resortId, ct).ConfigureAwait(false);
        var acksToday = await rulesStats.CountAcknowledgementsSinceAsync(resortId, todayStartUtc, ct).ConfigureAwait(false);

        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new DashboardStatsResponse(
            concierge.UnreadConversations,
            concierge.OpenConversations,
            openTickets,
            activeRooms,
            acksToday));
    }

    private static IResult Problem(Error error, HttpContext http) =>
        Results.Problem(ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http)));
}
