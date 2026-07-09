using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bedrock.Api.Observability;

/// <summary>
/// Wiring OpenTelemetry 3 trụ (F34/§9.3, R24.1): traces (HTTP server + nguồn <c>Bedrock</c>/<c>Npgsql</c>),
/// metrics (HTTP server + Meter <c>Bedrock</c>), logs (qua <c>ILogger</c> → OTel). Propagation W3C
/// <c>traceparent</c> là mặc định của OTel/ASP.NET → trace nối xuyên HTTP + (outbox <c>correlation_id</c> mang
/// traceparent). Exporter OTLP CHỈ bật khi có endpoint cấu hình (<c>Observability:Otlp:Endpoint</c>) — dev/test
/// KHÔNG export ra ngoài (span/metric vẫn tạo, kiểm được qua listener). Base cấp CƠ CHẾ; Host bật exporter thật.
/// </summary>
public static class BedrockObservabilityExtensions
{
    public static IServiceCollection AddBedrockObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var serviceName = configuration["Observability:ServiceName"]
            ?? Assembly.GetEntryAssembly()?.GetName().Name
            ?? "bedrock-service";
        var otlpEndpoint = configuration["Observability:Otlp:Endpoint"];
        var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(BedrockTelemetry.Name)
                    .AddSource("Npgsql") // EF/DB spans (Npgsql tự phát ActivitySource; chỉ cần tên, không ref SDK)
                    .AddAspNetCoreInstrumentation();
                if (hasOtlp)
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(BedrockTelemetry.Name)                    // metric nghiệp vụ/hạ tầng của base + component
                    .AddMeter("Microsoft.AspNetCore.RateLimiting")     // rate-limit rejects (R24.3)
                    .AddMeter("Microsoft.EntityFrameworkCore")         // EF query metrics (R24.3) — no-op nếu chưa phát
                    .AddMeter("Npgsql")                                // DB metrics provider (R24.3)
                    .AddAspNetCoreInstrumentation();                   // request rate/latency/error (R24.3)
                if (hasOtlp)
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            if (hasOtlp)
            {
                options.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint!));
            }
        }));

        return services;
    }
}
