using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Foundation.Api.Observability;

/// <summary>
/// Gắn CorrelationId (một sợi chỉ xuyên request — 19 §2): nhận từ header <c>X-Correlation-Id</c> nếu client
/// gửi (hợp lệ), ngược lại dùng <see cref="Activity"/>/TraceIdentifier. Đẩy vào Serilog LogContext để MỌI log
/// của request mang CorrelationId + set header response để client/đối soát.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var provided = context.Request.Headers[HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(provided) && provided.Length <= MaxLength)
        {
            return provided;
        }

        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
