using Foundation.Api.Configuration;
using Foundation.Api.ErrorHandling;
using Foundation.Api.Observability;
using Foundation.Api.RateLimiting;
using Foundation.Api.Security;
using Foundation.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foundation.Api;

/// <summary>
/// Điểm ráp toàn bộ nền cho host: Options (validate-on-start) + DI convention (Scrutor) + Auth/AuthZ + Security + Observability.
/// App tự thêm: EF DbContext + impl store/UnitOfWork của mình (base để trống — cần DB).
/// </summary>
public static class FoundationApiExtensions
{
    public static IServiceCollection AddFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddFoundationOptions(configuration);
        services.AddFoundationServices(); // Scrutor quét Application + Infrastructure
        services.AddFoundationAuth();
        services.AddFoundationSecurity(configuration);
        services.AddFoundationObservability();
        services.AddFoundationRateLimiting(configuration);

        return services;
    }

    /// <summary>
    /// Pipeline nền (thứ tự QUAN TRỌNG): exception → correlation/request-logging → HSTS → security headers → CORS → AuthN → AuthZ.
    /// LƯU Ý: KHÔNG bật HttpsRedirection ở đây — reverse proxy terminate TLS (DEC-025); app enable riêng nếu tự phục vụ TLS.
    /// </summary>
    public static IApplicationBuilder UseFoundation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseFoundationObservability(); // correlation id + Serilog request logging (path đã mask)
        app.UseHsts();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
