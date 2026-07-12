using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.Observability;

/// <summary>
/// Slot #2 pipeline (§3.5, F21/CP10, R24.2): chốt correlation id MỘT LẦN = traceId của trace hiện hành
/// (<see cref="CorrelationContext.CurrentTraceId"/>) vào <see cref="HttpContext.Items"/> + set response header
/// <c>X-Correlation-Id</c>. Mọi nơi (ProblemDetails, log) đọc lại qua <see cref="CorrelationContext.Resolve"/>
/// → header == traceId body == trace hiện hành.
/// <para>
/// KHÔNG nhận override từ header client: propagation xuyên service theo chuẩn W3C <c>traceparent</c> (OTel/ASP.NET
/// trích tự động → trace nối theo caller). Nhận id tuỳ tiện sẽ phá "== trace hiện hành" (đúng lỗ hổng F21 cần đóng).
/// </para>
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[CorrelationContext.ItemsKey] = CorrelationContext.CurrentTraceId(context);

        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            ctx.Response.Headers[CorrelationContext.HeaderName] = CorrelationContext.Resolve(ctx);
            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
