using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Bedrock.Api.Health;

/// <summary>
/// Health tách LIVENESS vs READINESS (R34/§9.6/N-015): <c>/health/live</c> = process còn sống (KHÔNG chạy check
/// dependency → orchestrator không restart oan khi dependency chập chờn); <c>/health/ready</c> = sẵn sàng nhận
/// traffic (chạy các check gắn tag <see cref="ReadyTag"/>, vd DB). Module/persistence đóng góp check qua tag.
/// </summary>
public static class HealthEndpoints
{
    public const string ReadyTag = "ready";

    public static void MapBedrockHealth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(ReadyTag) });
    }
}
