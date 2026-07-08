namespace Bedrock.Domain.Results;

/// <summary>
/// Kết quả thao tác CÓ giá trị <typeparamref name="T"/>. <see cref="Value"/> khi thất bại ném
/// <see cref="InvalidOperationException"/> (không trả <c>default</c> âm thầm — AD-002).
/// Implicit operator từ <typeparamref name="T"/>/<see cref="Error"/> để dùng return gọn ở use case.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    /// <summary>Giá trị khi thành công. Ném <see cref="InvalidOperationException"/> nếu truy cập lúc thất bại.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed Result.");

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, value, Error.None);
    }

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error == Error.None)
        {
            throw new ArgumentException("Failure result must carry a non-None error.", nameof(error));
        }

        return new Result<T>(false, default, error);
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
