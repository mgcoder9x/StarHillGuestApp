using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Behavior Transaction (§8) — behavior TRONG CÙNG trước use case, CHỈ áp cho <see cref="ICommandUseCase{TInput}"/>
/// (họ ghi thuần, AD-040): bọc thân trong <see cref="IUnitOfWork.ExecuteInTransactionAsync"/> để state + Outbox
/// commit all-or-nothing. Use case value-returning (<see cref="IUseCase{TInput,TOutput}"/>) — gồm cả query đọc và
/// command-trả-giá-trị — TỰ quản transaction tường minh khi cần (reentrancy R7.4 đảm bảo lời gọi lồng không xung đột),
/// tránh mở transaction thừa cho query chỉ đọc.
/// </summary>
public sealed class TransactionCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private readonly ICommandUseCase<TInput> _inner;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionCommandUseCaseDecorator(ICommandUseCase<TInput> inner, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _inner = inner;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default) =>
        _unitOfWork.ExecuteInTransactionAsync(
            token => _inner.ExecuteAsync(input, token),
            ct);
}
