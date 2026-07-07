namespace ResortQr.SharedKernel.Results;

/// <summary>
/// Tập mã lỗi GENERIC dùng chung mọi dự án (validation, not_found, conflict, ...).
/// Mã lỗi nghiệp vụ riêng (vd của resort) khai báo ở tầng app tương ứng, KHÔNG đặt ở base.
/// </summary>
public static class CommonErrors
{
    public static Error Validation(string message = "Dữ liệu không hợp lệ.")
        => Error.Validation("validation_error", message);

    public static Error NotFound(string message = "Không tìm thấy tài nguyên.")
        => Error.NotFound("not_found", message);

    public static Error Conflict(string message = "Xung đột trạng thái tài nguyên.")
        => Error.Conflict("conflict", message);

    public static Error Forbidden(string message = "Không có quyền thực hiện.")
        => Error.Forbidden("forbidden", message);

    public static Error Unauthorized(string message = "Chưa xác thực.")
        => Error.Unauthorized("unauthorized", message);

    public static Error ConcurrencyConflict(string message = "Dữ liệu đã thay đổi, vui lòng tải lại.")
        => Error.Conflict("concurrency_conflict", message);

    public static Error RateLimited(string message = "Thao tác quá nhanh, vui lòng thử lại sau.")
        => Error.RateLimited("rate_limited", message);

    public static Error Unexpected(string message = "Đã xảy ra lỗi không mong muốn.")
        => Error.Unexpected("unexpected", message);
}
