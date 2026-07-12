namespace Bedrock.Domain.Results;

/// <summary>
/// Lỗi nghiệp vụ trung lập: <see cref="Code"/> ổn định (hợp đồng máy-đọc với client), <see cref="Message"/>
/// là developer/neutral message (English) — localization cho người dùng thuộc app/UI layer theo
/// Accept-Language (F20). KHÔNG chứa HTTP status (tầng Api map <see cref="Type"/> → status).
/// <para>
/// P1-08: <see cref="Code"/>/<see cref="Message"/>/<see cref="Type"/> là GET-ONLY (không <c>init</c>) → invariant
/// "code/message không rỗng" của constructor KHÔNG bị bypass qua record <c>with { Code = "" }</c>. Chỉ
/// <see cref="Details"/> còn <c>init</c> để <see cref="WithDetails"/> clone (không đụng ba field core bất biến).
/// </para>
/// </summary>
public sealed record Error
{
    public Error(string code, string message, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Type = type;
    }

    private Error(string code, string message, ErrorType type, bool allowEmpty)
    {
        _ = allowEmpty;
        Code = code;
        Message = message;
        Type = type;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    /// <summary>Giá trị "không có lỗi" — dùng nội bộ cho <c>Result.Success()</c> (Error = None).</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure, allowEmpty: true);

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
    public static Error RateLimited(string code, string message) => new(code, message, ErrorType.RateLimited);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Failure);
}
