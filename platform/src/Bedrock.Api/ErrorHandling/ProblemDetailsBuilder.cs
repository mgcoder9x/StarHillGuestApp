using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace Bedrock.Api.ErrorHandling;

/// <summary>
/// Dựng RFC 7807 <see cref="ProblemDetails"/> từ <see cref="Error"/> trung lập (F20/F21):
/// <list type="bullet">
/// <item><c>status</c> = <see cref="ErrorTypeToHttp"/> map.</item>
/// <item><c>title</c> = <see cref="Error.Message"/> (developer/neutral English — UI dịch theo <c>code</c>).</item>
/// <item>extension <c>code</c> = machine-readable stable code; <c>traceId</c> = correlationId (F21).</item>
/// <item>extension <c>errors</c> = field details (nếu là validation error).</item>
/// </list>
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
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = traceId;

        if (error.Details is not null)
        {
            problem.Extensions["errors"] = error.Details;
        }

        return problem;
    }
}
