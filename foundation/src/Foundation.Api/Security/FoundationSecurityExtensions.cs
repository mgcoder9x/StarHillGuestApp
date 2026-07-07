using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Foundation.Api.Security;

/// <summary>
/// Wiring CORS (allowlist) + HSTS + đăng ký <see cref="SecurityOptions"/> (Req 20).
/// CORS/HSTS cấu hình LAZY qua <see cref="IOptions{SecurityOptions}"/> (KHÔNG đọc config eager) để nhận
/// cấu hình đã merge — tránh lỗi "policy rỗng khi in-memory config merge sau" (bài học DEC-040).
/// </summary>
public static class FoundationSecurityExtensions
{
    private static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];

    public static IServiceCollection AddFoundationSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SecurityOptions>>().Value);

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<SecurityOptions>>((cors, securityOptions) =>
            {
                var security = securityOptions.Value;
                cors.AddDefaultPolicy(policy =>
                {
                    if (security.AllowedCorsOrigins.Length > 0)
                    {
                        // Origin tường minh + credentials (KHÔNG wildcard) — Req 20.1/20.2.
                        policy.WithOrigins(security.AllowedCorsOrigins)
                            .WithMethods(AllowedMethods)
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                    // Rỗng → không origin nào → mặc định từ chối cross-origin (same-origin deploy).
                });
            });

        services.AddOptions<HstsOptions>()
            .Configure<IOptions<SecurityOptions>>((hsts, securityOptions) =>
            {
                hsts.MaxAge = TimeSpan.FromSeconds(securityOptions.Value.HstsMaxAgeSeconds);
                hsts.IncludeSubDomains = true;
            });

        return services;
    }
}
