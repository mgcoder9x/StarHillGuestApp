using ResortQr.Api.Configuration;
using ResortQr.Api.ErrorHandling;
using ResortQr.Api.Observability;
using ResortQr.Api.RateLimiting;
using ResortQr.Api.Security;
using ResortQr.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResortQr.Api;

/// <summary>
/// Điểm ráp toàn bộ nền cho host: Options (validate-on-start) + DI convention (Scrutor) + Auth/AuthZ + Security + Observability.
/// App tự thêm: EF DbContext + impl store/UnitOfWork của mình (base để trống — cần DB).
/// </summary>
public static class ResortQrApiExtensions
{
    public static IServiceCollection AddResortQr(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddResortQrOptions(configuration);
        services.AddResortQrServices(); // Scrutor quét Application + Infrastructure
        services.AddResortQrAuth();
        services.AddResortQrSecurity(configuration);
        services.AddResortQrObservability();
        services.AddResortQrRateLimiting(configuration);

        return services;
    }

    /// <summary>
    /// Pipeline nền (thứ tự QUAN TRỌNG): exception → correlation/request-logging → HSTS → security headers → CORS → AuthN → AuthZ.
    /// LƯU Ý: KHÔNG bật HttpsRedirection ở đây — reverse proxy terminate TLS (DEC-025); app enable riêng nếu tự phục vụ TLS.
    /// </summary>
    public static IApplicationBuilder UseResortQr(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseResortQrObservability(); // correlation id + Serilog request logging (path đã mask)
        app.UseHsts();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
