using Bedrock.Application.Ports.Caching;

namespace Bedrock.Infrastructure.Extensions.Defaults;

/// <summary>
/// Default DEGRADE-AN-TOÀN cho <see cref="IAppCache"/> khi chưa cắm adapter (§5.5/AD-009): mọi Get trả miss,
/// Set/Remove no-op. Cache miss vẫn cho kết quả ĐÚNG (chỉ chậm hơn) → an toàn để no-op. Khác hẳn các port
/// fail-loud (email/file/lock) — mất chúng âm thầm mới nguy hiểm.
/// </summary>
public sealed class NullAppCache : IAppCache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
}
