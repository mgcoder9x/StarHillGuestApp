namespace ResortQr.SharedKernel.Results;

/// <summary>
/// Kết quả thao tác KHÔNG có giá trị + là điểm factory tập trung cho cả <see cref="Result{T}"/>.
/// Dùng factory tường minh (không implicit operator) để rõ ràng + thân thiện analyzer.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>Lỗi khi thất bại; null khi thành công.</summary>
    public Error? Error { get; }

    public static Result Ok() => new(true, null);

    public static Result Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }

    /// <summary>Tạo <see cref="Result{T}"/> thành công (suy luận kiểu từ <paramref name="value"/>).</summary>
    public static Result<T> Ok<T>(T value) => new(true, value, null);

    /// <summary>Tạo <see cref="Result{T}"/> thất bại.</summary>
    public static Result<T> Fail<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, default!, error);
    }
}
