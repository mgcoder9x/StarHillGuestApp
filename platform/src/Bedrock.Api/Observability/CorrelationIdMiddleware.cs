using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.Observability;

/// <summary>
/// Slot #2 pipeline (§3.5, F21/CP10): resolve correlation id MỘT LẦN cho request và lưu vào
/// <see cref="HttpContext.Items"/> (<see cref="CorrelationContext.ItemsKey"/>) + set response header
/// <c>X-Correlation-Id</c>. Mọi nơi (ProblemDetails, log) đọc lại qua <see cref="CorrelationContext.Resolve"/>
/// → header == traceId body == trace hiện hành, KHÔNG lệch. Client có thể truyền sẵn id (đã validate).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveIncoming(context) ?? Activity.Current?.Id ?? context.TraceIdentifier;
        context.Items[CorrelationContext.ItemsKey] = correlationId;

        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var id = CorrelationContext.Resolve(ctx);
            ctx.Response.Headers[CorrelationContext.HeaderName] = id;
            return Task.CompletedTask;
        }, context);

        await next(context);
    }

    /// <summary>Chấp nhận id client gửi nếu an toàn (độ dài ≤128, chỉ ký tự an toàn) — chống log/header forging.</summary>
    private static string? ResolveIncoming(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationContext.HeaderName, out var values))
        {
            return null;
        }

        var candidate = values.ToString();
        if (string.IsNullOrEmpty(candidate) || candidate.Length > MaxLength)
        {
            return null;
        }

        foreach (var ch in candidate)
        {
            var ok = char.IsLetterOrDigit(ch) || ch is '-' or '.' or ':' or '_';
            if (!ok)
            {
                return null;
            }
        }

        return candidate;
    }
}
