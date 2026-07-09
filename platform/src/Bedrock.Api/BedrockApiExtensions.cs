using Bedrock.Api.Authentication;
using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Health;
using Bedrock.Api.HttpSecurity;
using Bedrock.Api.Observability;
using Bedrock.Api.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bedrock.Api;

/// <summary>
/// Umbrella đăng ký + pipeline cho tầng Api (design §6): CƠ CHẾ HTTP thuần, KHÔNG Infrastructure (F14).
/// <c>AddBedrockApi</c> đăng ký ở Host; <c>UseBedrockApi</c> áp thứ tự middleware §3.5 (các slot Bedrock sở hữu)
/// + map health + discovery <see cref="IEndpointModule"/>. Slot ForwardedHeaders/CORS/RateLimiter (F16/F17)
/// thêm ở task 11 tại đúng vị trí đã đánh dấu.
/// </summary>
public static class BedrockApiExtensions
{
    public static IServiceCollection AddBedrockApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBedrockAuthCore(configuration);
        services.AddBedrockHttpSecurity(configuration); // F16/F17: ForwardedHeaders + rate-limit + CORS (task 11).

        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName));
        services.AddSingleton(sp => new PathMasker(sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value));

        services.AddBedrockObservability(configuration); // F34/R24: OpenTelemetry 3 trụ + W3C traceparent.

        services.AddRouting();
        services.AddBedrockApiVersioning(); // F32/R22.1: URL-segment /v{version}, default v1, report versions.
        services.AddHealthChecks();
        return services;
    }

    /// <summary>Áp pipeline §3.5 (slot Bedrock sở hữu) + map health + module endpoints. Host gọi một dòng.</summary>
    public static IApplicationBuilder UseBedrockApi(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseForwardedHeaders();                          // #1 — IP/scheme thật cho mọi middleware sau (F16).
        app.UseMiddleware<CorrelationIdMiddleware>();       // #2
        app.UseMiddleware<ExceptionHandlingMiddleware>();   // #3
        // #4 HSTS / HTTPS redirect — TRÁCH NHIỆM HOST (deployment/TLS; base có thể chạy sau proxy terminate TLS).
        app.UseMiddleware<SecurityHeadersMiddleware>();     // #5
        app.UseMiddleware<RequestLoggingMiddleware>();      // #6
        app.UseRouting();                                    // #7
        app.UseCors(app.ApplicationServices.GetRequiredService<HttpSecurityOptions>().CorsPolicyName); // #8 (F17)
        app.UseRateLimiter();                               // #9 — partition theo IP thật đã resolve ở #1 (F16).
        app.UseAuthentication();                             // #10
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>                        // #11
        {
            endpoints.MapBedrockHealth();
            foreach (var module in endpoints.ServiceProvider.GetServices<IEndpointModule>())
            {
                module.MapEndpoints(endpoints);
            }
        });

        return app;
    }
}
