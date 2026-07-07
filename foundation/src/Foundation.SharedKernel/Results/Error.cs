namespace Foundation.SharedKernel.Results;

/// <summary>
/// Lỗi nghiệp vụ trung lập: <see cref="Code"/> ổn định (hợp đồng với client), <see cref="Message"/>
/// mô tả cho người đọc, <see cref="Type"/> để tầng Api ánh xạ HTTP. KHÔNG chứa status/header HTTP.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Chi tiết lỗi theo field (dùng cho validation — Req 12.2). Null nếu không có.</summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    /// <summary>Trả bản sao có kèm field-errors (immutable).</summary>
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
    public static Error RateLimited(string code, string message) => new(code, message, ErrorType.RateLimited);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
