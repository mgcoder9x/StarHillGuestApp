namespace Bedrock.Domain.Results;

/// <summary>
/// Kết quả thao tác KHÔNG có giá trị. Factory tường minh (không implicit operator) — rõ ràng, dễ đọc.
/// QUYẾT ĐỊNH ĐÃ CHỐT (AD-002): <c>sealed class</c> — KHÔNG <c>readonly struct</c>. Platform là web API
/// I/O-bound nên một cấp phát gen0/kết quả không đáng kể so với latency DB/HTTP, trong khi struct mang
/// bẫy <c>default(Result)</c> (trạng thái rỗng bị coi là hợp lệ) — footgun không cần thiết.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary><see cref="Results.Error.None"/> khi <see cref="IsSuccess"/>.</summary>
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error == Error.None)
        {
            throw new ArgumentException("Failure result must carry a non-None error.", nameof(error));
        }

        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}
