using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bedrock.Api.Health;

/// <summary>
/// Health tách LIVENESS vs READINESS (R34/§9.6/N-015): <c>/health/live</c> = process còn sống (KHÔNG chạy check
/// dependency → orchestrator không restart oan khi dependency chập chờn); <c>/health/ready</c> = sẵn sàng nhận
/// traffic (chạy các check gắn tag <see cref="ReadyTag"/>, vd DB). Module/persistence đóng góp check qua tag.
/// <para>
/// <c>/health/ready</c> phát body <c>application/json</c> liệt kê từng check + danh sách check thất bại (thay writer
/// plain-text mặc định) để orchestrator/người vận hành đọc được NGAY nguyên nhân 503 mà không cần bật debug. Payload
/// domain-agnostic (chỉ tên+trạng thái check do tầng dưới đăng ký) — lõi KHÔNG hardcode định danh nghiệp vụ.
/// </para>
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
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(ReadyTag),
                ResponseWriter = WriteReadyJsonAsync,
            });
    }

    private static Task WriteReadyJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new HealthReadyResponse(
            report.Status.ToString(),
            [.. report.Entries.Select(entry => new HealthCheckEntry(entry.Key, entry.Value.Status.ToString()))],
            [.. report.Entries.Where(entry => entry.Value.Status != HealthStatus.Healthy).Select(entry => entry.Key)]);

        return context.Response.WriteAsJsonAsync(payload);
    }

    private sealed record HealthReadyResponse(
        string Status,
        IReadOnlyList<HealthCheckEntry> Checks,
        IReadOnlyList<string> Failed);

    private sealed record HealthCheckEntry(string Name, string Status);
}
