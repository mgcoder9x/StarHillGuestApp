using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>
/// Behavior Idempotency (§8): nếu input implement <see cref="IIdempotentCommand"/> → gọi
/// <see cref="IIdempotencyStore.TryBeginAsync"/>. Lần đầu (true) → chạy thân; trùng key (false) → trả
/// <c>idempotency_conflict</c> (Conflict) — v1 KHÔNG replay response (tránh serialize response vào store, AD-039).
/// Input KHÔNG idempotent → pass-through (không chạm store — không tốn round-trip). TTL = <see cref="IdempotencyDefaults.Ttl"/>.
/// </summary>
public sealed class IdempotencyUseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
{
    private readonly IUseCase<TInput, TOutput> _inner;
    private readonly IIdempotencyStore _store;
    private readonly ICurrentUser _currentUser;

    public IdempotencyUseCaseDecorator(
        IUseCase<TInput, TOutput> inner,
        IIdempotencyStore store,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(currentUser);
        _inner = inner;
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<Result<TOutput>> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        if (input is IIdempotentCommand command)
        {
            var key = IdempotencyKeyScope.For<TInput>(command.IdempotencyKey, _currentUser);
            var isFirst = await _store.TryBeginAsync(key, IdempotencyDefaults.Ttl, ct).ConfigureAwait(false);
            if (!isFirst)
            {
                return Result<TOutput>.Failure(IdempotencyDefaults.Conflict);
            }
            try
            {
                var result = await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await _store.CompleteAsync(key, IdempotencyDefaults.Ttl, ct).ConfigureAwait(false);
                }
                else
                {
                    await _store.AbortAsync(key, ct).ConfigureAwait(false);
                }

                return result;
            }
            catch
            {
                await _store.AbortAsync(key, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }

        return await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
    }
}
