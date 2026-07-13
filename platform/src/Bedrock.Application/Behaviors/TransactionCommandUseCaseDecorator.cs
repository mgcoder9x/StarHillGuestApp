using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Behavior Transaction (§8) cho command không trả payload. Module keyed khai báo <see cref="ITransactionalUseCase"/>
/// để resolver chọn đúng Unit of Work; command legacy không marker dùng registration unkeyed. State và Outbox được
/// commit all-or-nothing trong transaction ngoài cùng của pipeline.
/// </summary>
public sealed class TransactionCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private readonly ICommandUseCase<TInput> _inner;
    private readonly IUnitOfWorkResolver _unitOfWorkResolver;

    public TransactionCommandUseCaseDecorator(
        ICommandUseCase<TInput> inner,
        IUnitOfWorkResolver unitOfWorkResolver)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(unitOfWorkResolver);
        _inner = inner;
        _unitOfWorkResolver = unitOfWorkResolver;
    }

    public Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        // PersistenceKey compile-enforced trên ICommandUseCase<TInput> (không còn cast tuỳ chọn) → module keyed
        // luôn resolve đúng Unit of Work; null = host một DbContext legacy (tường minh).
        return _unitOfWorkResolver.Resolve(_inner.PersistenceKey).ExecuteInTransactionAsync(
            token => _inner.ExecuteAsync(input, token),
            ct);
    }

    public string? PersistenceKey => _inner.PersistenceKey;
}
