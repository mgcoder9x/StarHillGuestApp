namespace Bedrock.Domain.Results;

/// <summary>
/// Vi phạm ràng buộc DUY NHẤT (unique constraint) khi ghi: một index/constraint UNIQUE bị đụng
/// (vd trùng số phòng, trùng token đang active). Ném ở tầng persistence (UnitOfWork bắt exception
/// provider-specific — Npgsql <c>PostgresException</c> SQLSTATE 23505 — rồi chuyển sang exception TRUNG LẬP
/// này) để tầng Application (⊥ EF) BẮT ĐƯỢC và ánh xạ sang <see cref="Error"/> nghiệp vụ; tầng Api map 409.
/// <para>
/// Bản chất: song song <see cref="ConcurrencyConflictException"/> — giữ Domain/Application/Api sạch, contract
/// KHÔNG rò rỉ chi tiết EF/Npgsql (design §5.1/persistence §3/§7). <see cref="ConstraintName"/> (nếu provider
/// cung cấp) cho phép use case phân biệt ràng buộc nào bị vi phạm (vd <c>ux_external_id</c> vs <c>ux_active_token</c>).
/// </para>
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    private const string DefaultMessage =
        "A unique constraint was violated by the write operation.";

    public UniqueConstraintViolationException()
        : base(DefaultMessage)
    {
    }

    public UniqueConstraintViolationException(string? message)
        : base(message ?? DefaultMessage)
    {
    }

    public UniqueConstraintViolationException(string? message, Exception innerException)
        : base(message ?? DefaultMessage, innerException)
    {
    }

    /// <summary>Tên ràng buộc/index bị vi phạm (nếu provider cung cấp — có thể null).</summary>
    public string? ConstraintName { get; init; }
}
