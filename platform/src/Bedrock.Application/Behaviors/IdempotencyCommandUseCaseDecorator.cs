using System.Security.Cryptography;
using System.Text;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Users;
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

internal static class IdempotencyKeyScope
{
    public static string For<TInput>(string rawKey, ICurrentUser currentUser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawKey);
        ArgumentNullException.ThrowIfNull(currentUser);

        // P1-03 (AD-092): tenant VÀ user là HAI CHIỀU ĐỘC LẬP (KHÔNG fallback `??`). Fallback cũ (Tenant ?? User)
        // bỏ UserId khi có tenant → hai user CÙNG tenant + CÙNG rawKey đụng key (một người chặn người kia). Giờ cả
        // hai chiều đều vào key. Anonymous (cả hai null) → "u=anon" (hạn chế đã biết: caller ẩn danh chung namespace;
        // endpoint cần idempotency cho ẩn danh phải cấp client/session key ổn định — ngoài phạm vi decorator).
        var tenant = currentUser.TenantId?.ToString("N") ?? "-";
        var user = currentUser.UserId?.ToString("N") ?? "anon";

        // Hash rawKey (SHA-256, hex thường): BOUND độ dài + KHÔNG lưu raw key (có thể chứa dữ liệu nhạy cảm) vào
        // store/log. Deterministic → cùng (TInput, tenant, user, rawKey) luôn ra cùng key.
        var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

        return $"{typeof(TInput).FullName}:t={tenant}:u={user}:{keyHash}";
    }
}

/// <summary>
/// Như <see cref="IdempotencyUseCaseDecorator{TInput,TOutput}"/> nhưng cho <see cref="ICommandUseCase{TInput}"/>.
/// </summary>
public sealed class IdempotencyCommandUseCaseDecorator<TInput> : ICommandUseCase<TInput>
{
    private readonly ICommandUseCase<TInput> _inner;
    private readonly IIdempotencyStore _store;
    private readonly ICurrentUser _currentUser;

    public IdempotencyCommandUseCaseDecorator(
        ICommandUseCase<TInput> inner,
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

    public async Task<Result> ExecuteAsync(TInput input, CancellationToken ct = default)
    {
        if (input is IIdempotentCommand command)
        {
            var key = IdempotencyKeyScope.For<TInput>(command.IdempotencyKey, _currentUser);
            var isFirst = await _store.TryBeginAsync(key, IdempotencyDefaults.Ttl, ct).ConfigureAwait(false);
            if (!isFirst)
            {
                return Result.Failure(IdempotencyDefaults.Conflict);
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

    public string? PersistenceKey => _inner.PersistenceKey;
}
