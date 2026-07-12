namespace Bedrock.Domain.Results;

/// <summary>
/// Xung đột ghi đồng thời (optimistic concurrency): bản ghi đã bị người/tiến trình khác thay đổi
/// giữa lúc đọc và lúc ghi. Ném ở tầng persistence (UnitOfWork bắt exception concurrency của provider
/// rồi chuyển sang exception TRUNG LẬP này) để tầng Api map sang HTTP 409 mà KHÔNG phụ thuộc ORM/provider.
/// Bản chất: giữ Domain/Api sạch — contract không rò rỉ chi tiết EF/Npgsql (design §5.1/persistence §3/§7).
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    private const string DefaultMessage =
        "The record was modified by another operation. Reload and try again.";

    public ConcurrencyConflictException()
        : base(DefaultMessage)
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
