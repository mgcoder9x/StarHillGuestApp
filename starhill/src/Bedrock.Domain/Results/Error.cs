namespace Bedrock.Domain.Results;

/// <summary>
/// Lỗi nghiệp vụ trung lập: <see cref="Code"/> ổn định (hợp đồng máy-đọc với client), <see cref="Message"/>
/// là developer/neutral message (English) — localization cho người dùng thuộc app/UI layer theo
/// Accept-Language (F20). KHÔNG chứa HTTP status (tầng Api map <see cref="Type"/> → status).
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Giá trị "không có lỗi" — dùng nội bộ cho <c>Result.Success()</c> (Error = None).</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Chi tiết lỗi theo field (validation). Null nếu không có.</summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    public Error WithDetails(IReadOnlyDictionary<string, string[]> details)
    {
        ArgumentNullException.ThrowIfNull(details);
        return this with { Details = details };
    }

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Failure);
}
