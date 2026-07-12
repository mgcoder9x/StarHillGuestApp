using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.Observability;

/// <summary>
/// Nguồn DUY NHẤT của correlation id cho một request (F21/CP10, R24.2): id chính tắc = <b>W3C traceId của
/// trace hiện hành</b> (<see cref="Activity.Current"/>). <see cref="CorrelationIdMiddleware"/> chốt 1 lần vào
/// <see cref="HttpContext.Items"/>; ProblemDetails/log/401-403 events đọc lại qua <see cref="Resolve"/> →
/// header <c>X-Correlation-Id</c> == <c>traceId</c> trong body == trace hiện hành, KHÔNG lệch.
/// <para>
/// Propagation xuyên service dùng chuẩn W3C <c>traceparent</c> (OTel/ASP.NET lo) → traceId tự nối theo caller.
/// KHÔNG nhận id tuỳ tiện từ header client (sẽ phá tính "== trace hiện hành" — chính lỗ hổng F21).
/// </para>
/// </summary>
public static class CorrelationContext
{
    public const string HeaderName = "X-Correlation-Id";

    internal const string ItemsKey = "Bedrock:CorrelationId";

    /// <summary>Trả correlation id đã chốt ở middleware; nếu chưa có, tính từ trace hiện hành.</summary>
    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(ItemsKey, out var value)
            && value is string existing
            && existing.Length > 0)
        {
            return existing;
        }

        return CurrentTraceId(context);
    }

    /// <summary>
    /// traceId của trace hiện hành (32-hex W3C). Fallback <see cref="HttpContext.TraceIdentifier"/> khi CHƯA có
    /// <see cref="Activity"/> (vd chưa wire tracing) — vẫn cho một id ổn định để không vỡ header/body.
    /// </summary>
    internal static string CurrentTraceId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
