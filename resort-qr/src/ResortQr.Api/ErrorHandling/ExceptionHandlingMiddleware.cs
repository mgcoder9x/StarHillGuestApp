using System.Text.Json;
using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ResortQr.Api.ErrorHandling;

/// <summary>
/// Middleware bắt exception CHƯA xử lý → ProblemDetails + traceId, KHÔNG lộ stack trace/chi tiết nội bộ (Req 4.5):
/// <see cref="ConcurrencyConflictException"/> → 409 concurrency_conflict; còn lại → 500 unexpected.
/// Client hủy (OperationCanceled) → không trả body.
/// </summary>
public sealed partial class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client đã ngắt kết nối → không phát response mới, chỉ ghi debug (không phải lỗi hệ thống).
            Log.RequestCanceled(_logger);
        }
        catch (ConcurrencyConflictException ex)
        {
            // Optimistic concurrency (UoW chuyển từ DbUpdateConcurrencyException của EF): 409, KHÔNG phải 500.
            // Api chỉ biết exception TRUNG LẬP của SharedKernel → không rò rỉ EF lên contract HTTP.
            Log.ConcurrencyConflict(_logger, ex);

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, CommonErrors.ConcurrencyConflict());
        }
#pragma warning disable CA1031 // Last-resort handler: BẮT BUỘC catch mọi exception để không lộ chi tiết + luôn trả ProblemDetails.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Log.UnhandledException(_logger, ex, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw; // Đã bắt đầu ghi response → không thể ghi đè; để tầng trên xử lý.
            }

            await WriteProblemAsync(context, CommonErrors.Unexpected());
        }
    }

    /// <summary>Chuẩn hóa ghi lỗi ra ProblemDetails (application/problem+json) + traceId. Gọi khi response CHƯA start.</summary>
    private static async Task WriteProblemAsync(HttpContext context, Error error)
    {
        var problem = ProblemDetailsBuilder.Build(error, ProblemDetailsBuilder.TraceId(context));

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", context.RequestAborted);
    }

    // Logging hiệu năng cao (source-generated) — chuẩn commercial, tránh box/allocation mỗi log.
    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Debug, Message = "Request bị hủy bởi client.")]
        public static partial void RequestCanceled(ILogger logger);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Xung đột concurrency khi ghi dữ liệu.")]
        public static partial void ConcurrencyConflict(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception khi xử lý {Path}")]
        public static partial void UnhandledException(ILogger logger, Exception exception, string path);
    }
}
