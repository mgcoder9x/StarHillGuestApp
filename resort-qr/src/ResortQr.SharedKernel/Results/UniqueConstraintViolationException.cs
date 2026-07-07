namespace ResortQr.SharedKernel.Results;

/// <summary>
/// Vi phạm ràng buộc UNIQUE ở DB (vd trùng số phòng, trùng token). Ném ở tầng persistence (UnitOfWork bắt
/// <c>DbUpdateException</c> của EF có inner là unique-violation của provider rồi chuyển sang exception TRUNG LẬP
/// này) để use case (Application) map sang <see cref="Error"/> nghiệp vụ mà KHÔNG phụ thuộc EF/Npgsql/SQLite.
/// <see cref="ConstraintName"/> có giá trị với Npgsql (production) để phân biệt ràng buộc nào; null với provider
/// không cung cấp (vd SQLite) → use case dựa ngữ cảnh thao tác để quyết định.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, string? constraintName, Exception innerException)
        : base(message, innerException)
        => ConstraintName = constraintName;

    /// <summary>Tên ràng buộc UNIQUE bị vi phạm (nếu provider cung cấp); null nếu không xác định được.</summary>
    public string? ConstraintName { get; }
}
