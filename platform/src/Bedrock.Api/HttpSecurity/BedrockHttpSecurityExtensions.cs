using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Bedrock.Api.ErrorHandling;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Đăng ký HTTP hardening (F16/F17): ForwardedHeaders (tin proxy khai tường minh), rate-limit biên partition
/// theo IP thật, và CORS theo <see cref="CookieSameSiteMode"/>. Pipeline áp ở <c>UseBedrockApi</c> đúng slot §3.5
/// (#1 ForwardedHeaders, #8 CORS, #9 RateLimiter). HSTS/HTTPS-redirect (#4) là trách nhiệm Host (deployment/TLS).
/// </summary>
public static class BedrockHttpSecurityExtensions
{
    public static IServiceCollection AddBedrockHttpSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new HttpSecurityOptions();
        configuration.GetSection(HttpSecurityOptions.SectionName).Bind(options);
        HttpSecurityOptions.Validate(options);
        services.AddSingleton(options);

        services.Configure<CookiePolicyOptions>(cookie =>
        {
            cookie.HttpOnly = HttpOnlyPolicy.Always;
            cookie.Secure = CookieSecurePolicy.Always;
            cookie.MinimumSameSitePolicy = options.CookieSameSiteMode == CookieSameSiteMode.CrossSite
                ? SameSiteMode.None
                : SameSiteMode.Lax;
        });
        services.AddAntiforgery(antiforgery =>
        {
            antiforgery.HeaderName = "X-CSRF-TOKEN";
            antiforgery.Cookie.HttpOnly = true;
            antiforgery.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            antiforgery.Cookie.SameSite = options.CookieSameSiteMode == CookieSameSiteMode.CrossSite
                ? SameSiteMode.None
                : SameSiteMode.Lax;
        });

        ConfigureForwardedHeaders(services, options);
        ConfigureRateLimiter(services, options);
        ConfigureCors(services, options);

        return services;
    }

    // #1 ForwardedHeaders: CHỈ tin proxy/network khai tường minh (F16 — không tin mù X-Forwarded-*, chống spoof IP).
    private static void ConfigureForwardedHeaders(IServiceCollection services, HttpSecurityOptions options) =>
        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardLimit = options.ForwardLimit;
            forwarded.KnownProxies.Clear();
            forwarded.KnownIPNetworks.Clear();

            foreach (var proxy in options.KnownProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    forwarded.KnownProxies.Add(address);
                }
            }

            foreach (var network in options.KnownNetworks)
            {
                // .NET 10: dùng KnownIPNetworks (System.Net.IPNetwork); KnownNetworks + HttpOverrides.IPNetwork đã obsolete.
                if (System.Net.IPNetwork.TryParse(network, out var parsed))
                {
                    forwarded.KnownIPNetworks.Add(parsed);
                }
            }

            // P0-01/AD-091 (SECURITY): CHỈ bật xử lý X-Forwarded-* khi có ÍT NHẤT một proxy/network được khai tin cậy.
            // KHÔNG có → ForwardedHeaders.None → header bị BỎ QUA → client gọi trực tiếp KHÔNG spoof được IP/scheme
            // (rate-limit #9 partition theo IP kết nối THẬT). An-toàn-mặc-định, ĐỘC LẬP phiên bản framework — kiểm
            // chứng behavioral bằng ForwardedHeadersTrustTests (test empirical bắt được: trust-list rỗng vẫn honor XFF).
            var hasTrustedSource = forwarded.KnownProxies.Count > 0 || forwarded.KnownIPNetworks.Count > 0;
            forwarded.ForwardedHeaders = hasTrustedSource
                ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                : ForwardedHeaders.None;
        });

    // #9 RateLimiter: partition theo IP thật (RemoteIpAddress đã resolve ở #1) → 429 khi vượt.
    private static void ConfigureRateLimiter(IServiceCollection services, HttpSecurityOptions options) =>
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitioning.ResolveClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.RateLimitPermitLimit,
                        Window = TimeSpan.FromSeconds(options.RateLimitWindowSeconds),
                        QueueLimit = options.RateLimitQueueLimit,
                    }));

            limiter.OnRejected = static async (context, token) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await ProblemDetailsWriter.WriteAsync(context.HttpContext, CommonErrors.RateLimited())
                    .WaitAsync(token);
            };
        });

    // #8 CORS (F17): same-site → không mở cross-origin (mặc định an toàn); cross-site → chỉ origin khai + credentials.
    private static void ConfigureCors(IServiceCollection services, HttpSecurityOptions options) =>
        services.AddCors(cors => cors.AddPolicy(options.CorsPolicyName, policy =>
        {
            if (options.CookieSameSiteMode == CookieSameSiteMode.CrossSite && options.CorsAllowedOrigins.Count > 0)
            {
                policy
                    .WithOrigins([.. options.CorsAllowedOrigins])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }

            // same-site: policy KHÔNG cấp header cross-origin → trình duyệt chặn request cross-site (đúng ý F17).
        }));
}
