using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;

namespace Foundation.Api.Observability;

/// <summary>
/// Observability nền (Req 14): structured logging (Serilog JSON) + correlation + health check.
/// Mask bí mật (Req 14.2): request-logging KHÔNG ghi header (Authorization/Cookie) + mask token trong path.
/// </summary>
public static class FoundationObservabilityExtensions
{
    /// <summary>Serilog JSON + health checks. Serilog không log header mặc định (tránh lộ Authorization/Cookie).</summary>
    public static IServiceCollection AddFoundationObservability(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSerilog((_, config) => config
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter(renderMessage: true)));

        services.AddHealthChecks();

        return services;
    }

    /// <summary>Correlation + request logging (path đã mask token). Gọi SỚM để bao trọn request.</summary>
    public static IApplicationBuilder UseFoundationObservability(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            // Template KHÔNG chứa RequestPath gốc → dùng path đã mask để không lộ token (Req 14.2).
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPathMasked} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnostic, httpContext) =>
                diagnostic.Set("RequestPathMasked", PathMasker.Mask(httpContext.Request.Path.Value));
        });

        return app;
    }

    /// <summary>
    /// <c>/health/live</c> (liveness): chỉ kiểm tiến trình, KHÔNG phụ thuộc DB (Req 14.3).
    /// <c>/health/ready</c> (readiness): chạy các check gắn tag <c>ready</c> (vd DB connectivity do
    /// <c>AddFoundationPersistence</c> đăng ký) — Req 14.6/14.7. Nếu app chưa wire persistence thì không có
    /// check nào tag "ready" → trả 200 (sẵn sàng, không có phụ thuộc ngoài để chờ).
    /// </summary>
    public static IEndpointRouteBuilder MapFoundationHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // không chạy check nào → chỉ xác nhận tiến trình sống
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
        }).AllowAnonymous();

        return endpoints;
    }
}
