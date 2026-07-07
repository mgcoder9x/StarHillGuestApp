using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Foundation.Api.ErrorHandling;
using Foundation.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Foundation.Api.RateLimiting;

/// <summary>
/// Rate limiting nền (Req 13): fixed-window partition theo IP; vượt ngưỡng → 429 ProblemDetails
/// (code=rate_limited) + Retry-After. Ngưỡng đọc LAZY qua <see cref="RateLimitOptions"/> trong partition
/// factory (mỗi request) — tránh đọc config eager (bài học DEC-040/042).
/// </summary>
public static class FoundationRateLimitExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddFoundationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RateLimitOptions>>().Value);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problem = ProblemDetailsBuilder.Build(CommonErrors.RateLimited(), ProblemDetailsBuilder.TraceId(httpContext));
                httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status429TooManyRequests;
                await httpContext.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
            };

            options.AddPolicy(AuthPolicy, httpContext =>
            {
                // LAZY: đọc ngưỡng từ DI mỗi request (không capture eager lúc đăng ký).
                var limits = httpContext.RequestServices.GetRequiredService<RateLimitOptions>();
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}
