using Bedrock.Api.Authentication;
using Bedrock.Api.Endpoints;
using Bedrock.Api.ErrorHandling;
using Bedrock.Api.Health;
using Bedrock.Api.HttpSecurity;
using Bedrock.Api.Observability;
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

        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.SectionName));
        services.AddSingleton(sp => new PathMasker(sp.GetRequiredService<IOptions<ObservabilityOptions>>().Value));

        services.AddRouting();
        services.AddHealthChecks();
        return services;
    }

    /// <summary>Áp pipeline §3.5 (slot Bedrock sở hữu) + map health + module endpoints. Host gọi một dòng.</summary>
    public static IApplicationBuilder UseBedrockApi(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // #1 ForwardedHeaders — task 11 (PHẢI đứng đầu khi thêm: để IP/scheme thật cho các middleware sau).
        app.UseMiddleware<CorrelationIdMiddleware>();       // #2
        app.UseMiddleware<ExceptionHandlingMiddleware>();   // #3
        // #4 HSTS / HTTPS redirect — task 11.
        app.UseMiddleware<SecurityHeadersMiddleware>();     // #5
        app.UseMiddleware<RequestLoggingMiddleware>();      // #6
        app.UseRouting();                                    // #7
        // #8 CORS — task 11.  #9 RateLimiter — task 11 (partition theo IP thật ở #1).
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
