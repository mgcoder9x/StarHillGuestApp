using Bedrock.Api.Observability;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Bedrock.Api.ErrorHandling;

/// <summary>
/// Slot #3 pipeline (§3.5): bọc ngoài mọi lỗi phía sau. Map <see cref="ConcurrencyConflictException"/> và
/// <see cref="UniqueConstraintViolationException"/> (kernel, KHÔNG phụ thuộc EF — F5) → 409; lỗi khác → 500
/// trung lập. LOG path QUA MASKER (CP13/F15) — token không lọt vào log kể cả request lỗi 500 (chính lỗ hổng
/// F15). Response lỗi dựng qua <see cref="ProblemDetailsWriter"/>.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    PathMasker masker)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client đã ngắt kết nối: không phải lỗi server và không thể ghi response đáng tin cậy.
        }
        catch (ConcurrencyConflictException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteIfPossibleAsync(context, CommonErrors.Concurrency);
        }
        catch (UniqueConstraintViolationException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            // Uncaught unique-violation (use case không map thành Error riêng) → 409 chung. Module thường
            // BẮT UniqueConstraintViolationException để trả Error nghiệp vụ cụ thể (vd số phòng đã tồn tại).
            await WriteIfPossibleAsync(context, CommonErrors.Conflict());
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            if (logger.IsEnabled(LogLevel.Error))
            {
                var maskedPath = masker.Mask(context.Request.Path.Value);
                var correlationId = CorrelationContext.Resolve(context);
                ApiLog.UnhandledException(logger, ex, context.Request.Method, maskedPath, correlationId);
            }

            await WriteIfPossibleAsync(context, CommonErrors.Unexpected());
        }
    }

    private static async Task WriteIfPossibleAsync(HttpContext context, Error error)
    {
        context.Response.Clear();
        await ProblemDetailsWriter.WriteAsync(context, error);
    }
}
