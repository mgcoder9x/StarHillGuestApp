using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Bedrock.Api.Observability;

/// <summary>
/// Slot #6 pipeline (§3.5): log mỗi request với path ĐÃ MASK (CP13/F15) — dùng chung <see cref="PathMasker"/>
/// với exception handler nên token không lọt log ở bất kỳ đường nào. Sau correlation (#2), trước routing.
/// </summary>
public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    PathMasker masker)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var timestamp = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            // Chỉ tính path-mask/elapsed/correlation KHI level bật (CA1873 — tránh chi phí khi logging tắt).
            if (logger.IsEnabled(LogLevel.Information))
            {
                var elapsedMs = (long)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
                var maskedPath = masker.Mask(context.Request.Path.Value);
                var correlationId = CorrelationContext.Resolve(context);
                ApiLog.Request(
                    logger,
                    context.Request.Method,
                    maskedPath,
                    context.Response.StatusCode,
                    elapsedMs,
                    correlationId);
            }
        }
    }
}
