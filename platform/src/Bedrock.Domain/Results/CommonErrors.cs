namespace Bedrock.Domain.Results;

/// <summary>
/// Tập mã lỗi GENERIC dùng chung mọi dự án (validation, not_found, conflict, ...).
/// <b>Code ổn định</b> (hợp đồng máy-đọc, KHÔNG đổi) + <b>message developer/neutral (English)</b> — thông điệp
/// người-dùng được localize ở tầng app/UI theo <c>code</c> (F20). Mã lỗi nghiệp vụ riêng khai ở module/app, KHÔNG ở base.
/// </summary>
public static class CommonErrors
{
    public static Error Validation(string message = "One or more validation errors occurred.")
        => Error.Validation("validation_error", message);

    /// <summary>Not-found cho một loại thực thể cụ thể — code dạng <c>{entity}.not_found</c> (design §4.4).</summary>
    public static Error NotFound(string entity)
        => Error.NotFound($"{entity}.not_found", $"{entity} was not found.");

    /// <summary>Not-found chung, không gắn với entity cụ thể — code cố định <c>not_found</c>.</summary>
    public static Error NotFoundGeneric(string message = "The requested resource was not found.")
        => Error.NotFound("not_found", message);

    public static Error Conflict(string message = "The resource state conflicts with the request.")
        => Error.Conflict("conflict", message);

    public static Error Forbidden(string message = "You do not have permission to perform this action.")
        => Error.Forbidden("forbidden", message);

    public static Error Unauthorized(string message = "Authentication is required.")
        => Error.Unauthorized("unauthorized", message);

    /// <summary>Xung đột ghi đồng thời (optimistic concurrency) — dùng khi map <see cref="ConcurrencyConflictException"/>.</summary>
    public static readonly Error Concurrency =
        Error.Conflict("concurrency_conflict", "The resource was modified concurrently. Reload and try again.");

    public static Error RateLimited(string message = "Too many requests. Please try again later.")
        => new("rate_limited", message, ErrorType.Failure);

    public static Error Unexpected(string message = "An unexpected error occurred.")
        => Error.Unexpected("unexpected", message);
}
