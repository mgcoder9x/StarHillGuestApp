namespace ResortQr.SharedKernel.Results;

/// <summary>
/// Xung đột ghi đồng thời (optimistic concurrency): bản ghi đã bị người/tiến trình khác thay đổi
/// giữa lúc đọc và lúc ghi. Ném ở tầng persistence (UnitOfWork bắt <c>DbUpdateConcurrencyException</c>
/// của EF rồi chuyển sang exception TRUNG LẬP này) để tầng Api map sang HTTP 409 mà KHÔNG phụ thuộc EF.
/// Bản chất: giữ tầng Api sạch (chỉ biết SharedKernel), không rò rỉ chi tiết ORM lên contract HTTP.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("Dữ liệu đã thay đổi bởi thao tác khác, vui lòng tải lại và thử lại.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
