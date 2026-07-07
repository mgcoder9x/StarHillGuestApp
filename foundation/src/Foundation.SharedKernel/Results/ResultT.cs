namespace Foundation.SharedKernel.Results;

/// <summary>
/// Kết quả thao tác CÓ giá trị <typeparamref name="T"/>. Ok mang value; Fail mang <see cref="Results.Error"/>.
/// Khởi tạo qua factory ở <see cref="Result"/> (<c>Result.Ok(value)</c> / <c>Result.Fail&lt;T&gt;(error)</c>)
/// — tránh static member trên generic type (CA1000) + cho phép suy luận kiểu.
/// Truy cập <see cref="Value"/> khi thất bại sẽ ném <see cref="InvalidOperationException"/> (fail-fast).
/// </summary>
public sealed class Result<T>
{
    private readonly T _value;

    internal Result(bool isSuccess, T value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    /// <summary>Giá trị khi thành công. Ném nếu truy cập lúc thất bại.</summary>
    public T Value =>
        IsSuccess ? _value : throw new InvalidOperationException("Không thể đọc Value của một Result thất bại.");
}
