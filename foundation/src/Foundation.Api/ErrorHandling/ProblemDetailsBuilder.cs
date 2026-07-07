using System.Diagnostics;
using Foundation.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.Api.ErrorHandling;

/// <summary>
/// Dựng ProblemDetails đủ 5 trường hợp đồng (Req 4.2): <c>type</c> (/problems/{code}), <c>title</c>,
/// <c>status</c>, <c>code</c> (∈ catalog), <c>traceId</c> (correlation). Dùng chung cho endpoint result lẫn middleware.
/// </summary>
public static class ProblemDetailsBuilder
{
    public static ProblemDetails Build(Error error, string traceId)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = new ProblemDetails
        {
            Status = ErrorTypeToHttp.ToStatusCode(error.Type),
            Title = error.Message,
            Type = $"/problems/{error.Code}",
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = traceId;
        if (error.Details is not null)
        {
            problem.Extensions["errors"] = error.Details; // danh sách field lỗi (Req 12.2)
        }

        return problem;
    }

    /// <summary>CorrelationId cho response lỗi: ưu tiên Activity (distributed tracing) rồi tới TraceIdentifier.</summary>
    public static string TraceId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
