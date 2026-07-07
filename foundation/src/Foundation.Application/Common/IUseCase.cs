using Foundation.SharedKernel.Results;

namespace Foundation.Application.Common;

/// <summary>Marker cho use case (để log/test/quét đăng ký).</summary>
public interface IUseCase;

/// <summary>Use case CÓ input → trả <see cref="Result{TOutput}"/>.</summary>
public interface IUseCase<in TInput, TOutput> : IUseCase
{
    Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
}

/// <summary>Use case KHÔNG có output giá trị (chỉ Ok/Fail).</summary>
public interface ICommandUseCase<in TInput> : IUseCase
{
    Task<Result> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
}
