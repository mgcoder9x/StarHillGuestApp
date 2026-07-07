using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace ResortQr.Api.ErrorHandling;

/// <summary>Ánh xạ <see cref="ErrorType"/> (trung lập) → HTTP status (Req 4.3). Type ngoài bảng → 500 (Req 4.4).</summary>
public static class ErrorTypeToHttp
{
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
        _ => StatusCodes.Status500InternalServerError,
    };
}
