using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.Observability;

/// <summary>
/// Nguồn DUY NHẤT của correlation id cho một request (F21/CP10): middleware (task 5.4) resolve 1 lần và lưu
/// vào <see cref="HttpContext.Items"/> theo <see cref="ItemsKey"/>; mọi nơi cần traceId (ProblemDetails,
/// log, JWT 401/403 events) đọc lại qua <see cref="Resolve"/> → header <c>X-Correlation-Id</c> == traceId
/// trong body == trace hiện hành, không lệch nhau.
/// </summary>
public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-Id";

    internal const string ItemsKey = "Bedrock:CorrelationId";

    /// <summary>Trả correlation id đã chốt ở middleware; fallback Activity/TraceIdentifier khi chưa có.</summary>
    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemsKey, out var value)
            && value is string existing
            && !string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
