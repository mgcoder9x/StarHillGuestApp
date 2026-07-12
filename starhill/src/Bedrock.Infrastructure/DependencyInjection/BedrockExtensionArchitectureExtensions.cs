using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Email;
using Bedrock.Application.Ports.ExternalAuth;
using Bedrock.Application.Ports.Search;
using Bedrock.Application.Ports.Storage;
using Bedrock.Infrastructure.Extensions;
using Bedrock.Infrastructure.Extensions.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bedrock.Infrastructure.DependencyInjection;

/// <summary>
/// Khung Extension Architecture (F24, §6.1): cặp <c>AddXxxCore()</c> đăng ký DEFAULT AN TOÀN theo phân loại
/// §5.5 (degrade vs fail-loud) qua <c>TryAdd</c>. Adapter cụ thể (task 14, <c>Adapters.*</c>) OVERRIDE bằng
/// <c>services.Replace(...)</c> → luôn đúng MỘT registration (duplicate-guard task 10 không báo nhầm). Bỏ dòng
/// adapter = port quay về default (degrade hoặc fail-loud khi bị gọi — R16.4).
/// </summary>
public static class BedrockExtensionArchitectureExtensions
{
    /// <summary>Messaging core: default fail-loud <see cref="IEventBusPublisher"/> (chưa có bus adapter thì không publish âm thầm).</summary>
    public static IServiceCollection AddMessagingCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IEventBusPublisher, ThrowingEventBusPublisher>();
        return services;
    }

    /// <summary>Search core: default fail-loud open-generic index/query (§5.5/F26).</summary>
    public static IServiceCollection AddSearchCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ISearchIndex<>), typeof(ThrowingSearchIndex<>)));
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ISearchQuery<>), typeof(ThrowingSearchQuery<>)));
        return services;
    }

    /// <summary>Email core: default fail-loud <see cref="IEmailSender"/>.</summary>
    public static IServiceCollection AddEmailCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IEmailSender, ThrowingEmailSender>();
        return services;
    }

    /// <summary>
    /// Cache core: <see cref="IAppCache"/> DEGRADE (NullAppCache miss-through); lock/idempotency/rate-limit FAIL-LOUD
    /// (§5.5 — vắng chúng âm thầm gây race/double-run).
    /// </summary>
    public static IServiceCollection AddCacheCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IAppCache, NullAppCache>();                       // degrade an toàn.
        services.TryAddSingleton<IDistributedLock, ThrowingDistributedLock>();     // fail-loud.
        services.TryAddSingleton<IIdempotencyStore, ThrowingIdempotencyStore>();   // fail-loud.
        services.TryAddSingleton<IRateLimitStore, ThrowingRateLimitStore>();       // fail-loud.
        return services;
    }

    /// <summary>Storage core: default fail-loud <see cref="IFileStorage"/>.</summary>
    public static IServiceCollection AddStorageCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IFileStorage, ThrowingFileStorage>();
        return services;
    }

    /// <summary>
    /// External-auth core: registry resolve theo Name. <see cref="IExternalAuthProvider"/> là MULTI-IMPL có chủ
    /// đích (google/zalo/...) → whitelist khỏi duplicate-guard (task 10). Không có provider → Resolve fail-loud.
    /// </summary>
    public static IServiceCollection AddExternalAuthCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IExternalAuthProviderRegistry, ExternalAuthProviderRegistry>();
        services.BedrockStartupValidation().AllowMultipleImplementations(typeof(IExternalAuthProvider));
        return services;
    }
}
