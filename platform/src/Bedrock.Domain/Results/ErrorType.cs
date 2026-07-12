namespace Bedrock.Domain.Results;

/// <summary>Phân loại lỗi trung lập (KHÔNG chứa HTTP). Tầng Api ánh xạ sang HTTP status.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    RateLimited,
    Failure,
}
