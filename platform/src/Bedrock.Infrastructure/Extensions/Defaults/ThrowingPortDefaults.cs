using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Email;
using Bedrock.Application.Ports.Search;
using Bedrock.Application.Ports.Storage;

namespace Bedrock.Infrastructure.Extensions.Defaults;

/// <summary>
/// Thông điệp lỗi nhất quán cho port FAIL-LOUD chưa cắm adapter (§5.5/R16.4). Fail-loud có chủ đích: no-op âm
/// thầm (mất email/file/lock/message) nguy hiểm hơn crash — crash lộ ngay lúc dev/test.
/// </summary>
internal static class NoAdapterError
{
    public static InvalidOperationException For(string port, string capability) =>
        new($"Chưa đăng ký adapter cho '{port}'. Cắm adapter '{capability}' ở Host (vd Add<Tech>{capability}(cfg)). "
            + "Port này FAIL-LOUD có chủ đích (§5.5/R16.4) — KHÔNG no-op âm thầm.");
}

/// <summary>Default fail-loud <see cref="IEmailSender"/> — mất email âm thầm nguy hiểm (§5.5).</summary>
public sealed class ThrowingEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default) =>
        throw NoAdapterError.For("IEmailSender", "Email");
}

/// <summary>Default fail-loud <see cref="IFileStorage"/> — mất file âm thầm nguy hiểm (§5.5).</summary>
public sealed class ThrowingFileStorage : IFileStorage
{
    public Task<string> SaveAsync(FileBlob blob, CancellationToken ct = default) =>
        throw NoAdapterError.For("IFileStorage", "Storage");

    public Task<Stream> OpenAsync(string key, CancellationToken ct = default) =>
        throw NoAdapterError.For("IFileStorage", "Storage");
}

/// <summary>Default fail-loud <see cref="IDistributedLock"/> — mất lock = TÁI TẠO RACE (§5.5).</summary>
public sealed class ThrowingDistributedLock : IDistributedLock
{
    public Task<ILockHandle?> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct = default) =>
        throw NoAdapterError.For("IDistributedLock", "Cache");
}

/// <summary>Default fail-loud <see cref="IIdempotencyStore"/> (§5.5).</summary>
public sealed class ThrowingIdempotencyStore : IIdempotencyStore
{
    public Task<bool> TryBeginAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default) =>
        throw NoAdapterError.For("IIdempotencyStore", "Cache");

    public Task CompleteAsync(string idempotencyKey, TimeSpan ttl, CancellationToken ct = default) =>
        throw NoAdapterError.For("IIdempotencyStore", "Cache");

    public Task AbortAsync(string idempotencyKey, CancellationToken ct = default) =>
        throw NoAdapterError.For("IIdempotencyStore", "Cache");
}

/// <summary>Default fail-loud <see cref="IRateLimitStore"/> (distributed) (§5.5).</summary>
public sealed class ThrowingRateLimitStore : IRateLimitStore
{
    public Task<bool> TryAcquireAsync(string partition, int limit, TimeSpan window, CancellationToken ct = default) =>
        throw NoAdapterError.For("IRateLimitStore", "Cache");
}

/// <summary>Default fail-loud <see cref="IEventBusPublisher"/> — không có bus adapter thì KHÔNG publish âm thầm (F25).</summary>
public sealed class ThrowingEventBusPublisher : IEventBusPublisher
{
    public Task PublishAsync(OutgoingIntegrationMessage message, CancellationToken ct = default) =>
        throw NoAdapterError.For("IEventBusPublisher", "Messaging");
}

/// <summary>Default fail-loud <see cref="ISearchIndex{TDoc}"/> (open-generic) (§5.5/F26).</summary>
public sealed class ThrowingSearchIndex<TDoc> : ISearchIndex<TDoc>
{
    public Task IndexAsync(TDoc document, CancellationToken ct = default) =>
        throw NoAdapterError.For("ISearchIndex<T>", "Search");

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        throw NoAdapterError.For("ISearchIndex<T>", "Search");
}

/// <summary>Default fail-loud <see cref="ISearchQuery{TDoc}"/> (open-generic) (§5.5/F26).</summary>
public sealed class ThrowingSearchQuery<TDoc> : ISearchQuery<TDoc>
{
    public Task<SearchResult<TDoc>> SearchAsync(SearchRequest request, CancellationToken ct = default) =>
        throw NoAdapterError.For("ISearchQuery<T>", "Search");
}
