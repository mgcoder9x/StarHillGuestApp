using Bedrock.Application.Ports.Caching;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;

namespace Bedrock.Application.Behaviors;

/// <summary>Hằng dùng chung cho Idempotency behavior (§8, AD-039).</summary>
public static class IdempotencyDefaults
{
    /// <summary>
    /// TTL mặc định của một khoá idempotency (design §8 không chốt con số → chọn 24h: đủ dài phủ mọi retry
    /// hợp lý của client/gateway, đủ ngắn để store tự dọn — AD-039). Adapter có thể override khi cần.
    /// </summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    /// <summary>Lỗi trả khi phát hiện trùng khoá (mã ổn định <c>idempotency_conflict</c>, design §8).</summary>
    public static readonly Error Conflict =
        Error.Conflict("idempotency_conflict", "A request with the same idempotency key is already in progress or completed.");
}

/// <summary>
/// Như <see cref="IdempotencyUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>.
/// </summary>
public sealed class IdempotencyCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private readonly ICommandUseCase<TInput> _inner;
    private readonly IIdempotencyStore _store;

    public IdempotencyCommandUseCaseDecorator(ICommandUseCase<TInput> inner, IIdempotencyStore store)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(store);
        _inner = inner;
        _store = store;
    }

    public async Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        if (input is IIdempotentCommand command)
        {
            var isFirst = await _store.TryBeginAsync(command.IdempotencyKey, IdempotencyDefaults.Ttl, ct).ConfigureAwait(false);
            if (!isFirst)
            {
                return Result.Failure(IdempotencyDefaults.Conflict);
            }
        }

        return await _inner.ExecuteAsync(input, ct).ConfigureAwait(false);
    }
}
