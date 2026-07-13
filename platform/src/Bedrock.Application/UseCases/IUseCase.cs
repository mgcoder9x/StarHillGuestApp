using Bedrock.Domain.Results;

namespace Bedrock.Application.UseCases;

/// <summary>Marker cho use case (để log/test/quét đăng ký + luật ArchTest CP11).</summary>
#pragma warning disable CA1040 // Marker interface là chủ đích (phân loại use case cho pipeline + ArchTest).
public interface IUseCase;
#pragma warning restore CA1040

/// <summary>
/// Marker cho use case ghi dữ liệu cần transaction. <see cref="PersistenceKey"/> chọn đúng Unit of Work của
/// bounded context; <c>null</c> chỉ dành cho host một DbContext legacy.
/// </summary>
public interface ITransactionalUseCase : IUseCase
{
    string? PersistenceKey { get; }
}

/// <summary>
/// Service contract chung cho use case có payload trả về. Implementation nghiệp vụ nên chọn semantic interface
/// <see cref="IQueryUseCase{TInput,TOutput}"/> hoặc <see cref="ICommandUseCase{TInput,TOutput}"/>.
/// </summary>
public interface IUseCase<in TInput, TOutput> : IUseCase
{
    Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default);
}

/// <summary>Read-only use case; transaction decorator luôn pass-through.</summary>
public interface IQueryUseCase<in TInput, TOutput> : IUseCase<TInput, TOutput>;

/// <summary>Value-returning write use case; phải khai module persistence key để pipeline mở đúng transaction.</summary>
public interface ICommandUseCase<in TInput, TOutput> : IUseCase<TInput, TOutput>, ITransactionalUseCase;

/// <summary>
/// Use case ghi KHÔNG có output giá trị (chỉ Ok/Fail). Kế thừa <see cref="ITransactionalUseCase"/> → COMPILE-ENFORCE
/// khai <see cref="ITransactionalUseCase.PersistenceKey"/> (đối xứng với <see cref="ICommandUseCase{TInput,TOutput}"/>).
/// Host một DbContext legacy khai <c>PersistenceKey => null</c> (unkeyed) MỘT cách TƯỜNG MINH; module keyed trả module
/// key. Nhờ compile-enforce, không còn nguy cơ command keyed quên marker rồi resolve nhầm Unit of Work unkeyed lúc
/// runtime (khoá gốc A-... — không cần architecture test riêng để bắt).
/// </summary>
public interface ICommandUseCase<in TInput> : IUseCase, ITransactionalUseCase
{
    Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default);
}
