using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.ErrorHandling;

/// <summary>
/// Ánh xạ <see cref="ErrorType"/> (trung lập, ở Domain) → HTTP status. Đây là NƠI DUY NHẤT lõi biết status
/// code — Application/Domain KHÔNG biết HTTP (review §2, Error Handling). Giữ map ở Api để tầng dưới sạch.
/// </summary>
public static class ErrorTypeToHttp
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError,
    };
}
