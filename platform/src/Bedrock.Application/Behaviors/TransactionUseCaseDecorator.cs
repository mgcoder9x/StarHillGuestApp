using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Transaction behavior cho command trả giá trị. Query vẫn dùng <see cref="IUseCase{TInput,TOutput}"/> nhưng không
/// implement <see cref="ITransactionalUseCase"/>, nên đi thẳng và không mở transaction.
/// </summary>
public sealed class TransactionUseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private readonly IUseCase<TInput, TOutput> _inner;
    private readonly IUnitOfWorkResolver _unitOfWorkResolver;

    public TransactionUseCaseDecorator(
        IUseCase<TInput, TOutput> inner,
        IUnitOfWorkResolver unitOfWorkResolver)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(unitOfWorkResolver);
        _inner = inner;
        _unitOfWorkResolver = unitOfWorkResolver;
    }

    public Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        if (_inner is not ITransactionalUseCase transactional)
        {
            return _inner.ExecuteAsync(input, ct);
        }

        return _unitOfWorkResolver
            .Resolve(transactional.PersistenceKey)
            .ExecuteInTransactionAsync(token => _inner.ExecuteAsync(input, token), ct);
    }
}
