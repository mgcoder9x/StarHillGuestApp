using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ResortQr.Api.ErrorHandling;

/// <summary>Chuyển <see cref="Result"/>/<see cref="Result{T}"/> → <see cref="IResult"/> (minimal API): Ok/NoContent hoặc ProblemDetails.</summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return ToProblem(result.Error!, httpContext);
    }

    public static IResult ToHttpResult(this Result result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return ToProblem(result.Error!, httpContext);
    }

    private static ProblemHttpResult ToProblem(Error error, HttpContext httpContext)
    {
        var problem = ProblemDetailsBuilder.Build(error, ProblemDetailsBuilder.TraceId(httpContext));
        return TypedResults.Problem(problem);
    }
}
