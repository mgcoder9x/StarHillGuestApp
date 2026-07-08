using Bedrock.Api.Observability;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.ErrorHandling;

/// <summary>
/// Ghi <see cref="Error"/> ra HTTP response dưới dạng ProblemDetails — NƠI DUY NHẤT dựng response lỗi
/// (auth 401/403 events, exception handler đều gọi đây). Một nguồn → không drift shape/format lỗi. TraceId
/// lấy qua <see cref="CorrelationContext.Resolve"/> (F21/CP10). ContentType chuẩn RFC 7807.
/// </summary>
public static class ProblemDetailsWriter
{
    public static async Task WriteAsync(HttpContext http, Error error)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(error);

        var problem = ProblemDetailsBuilder.Build(error, CorrelationContext.Resolve(http));
        http.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await http.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json")
            .ConfigureAwait(false);
    }
}
