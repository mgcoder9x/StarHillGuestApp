using Bedrock.Domain.Results;

namespace Bedrock.Application.UseCases;

/// <summary>Marker cho use case (để log/test/quét đăng ký + luật ArchTest CP11).</summary>
#pragma warning disable CA1040 // Marker interface là chủ đích (phân loại use case cho pipeline + ArchTest).
public interface IUseCase;
#pragma warning restore CA1040

/// <summary>Use case CÓ input → trả <see cref="Result{TOutput}"/>.</summary>
public interface IUseCase<in TInput, TOutput> : IUseCase
{
    Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default);
}

/// <summary>Use case KHÔNG có output giá trị (chỉ Ok/Fail).</summary>
public interface ICommandUseCase<in TInput> : IUseCase
{
    Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default);
}
