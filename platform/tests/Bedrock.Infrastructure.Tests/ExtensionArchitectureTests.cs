using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Application.Ports.Caching;
using Bedrock.Application.Ports.Email;
using Bedrock.Application.Ports.ExternalAuth;
using Bedrock.Application.Ports.Search;
using Bedrock.Application.Ports.Storage;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Extensions.Defaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bedrock.Infrastructure.Tests;

// ── Test doubles ────────────────────────────────────────────────────────────
public sealed record SearchDoc(string Id);

public sealed class FakeEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class FakeAuthProvider(string name) : IExternalAuthProvider
{
    public string Name => name;

    public Task<ExternalAuthChallenge> CreateChallengeAsync(ExternalAuthRequest request, CancellationToken ct = default) =>
        Task.FromResult(new ExternalAuthChallenge(new Uri("https://provider.example/authorize"), "state"));

    public Task<ExternalUserProfile> CompleteAsync(ExternalAuthCallback callback, CancellationToken ct = default) =>
        Task.FromResult(new ExternalUserProfile(name, "provider-user-id", null, null, null));
}

/// <summary>
/// Task 13 — Extension Architecture: default AN TOÀN theo phân loại §5.5 (degrade vs fail-loud) + override qua
/// Replace + registry multi-impl. Chứng minh triết lý AD-009: "vắng adapter" có ngữ nghĩa khác nhau theo port.
/// </summary>
public sealed class ExtensionArchitectureTests
{
    [Fact]
    public async Task Cache_core_degrades_safely_miss_through()
    {
        using var provider = new ServiceCollection().AddCacheCore().BuildServiceProvider();
        var cache = provider.GetRequiredService<IAppCache>();

        Assert.IsType<NullAppCache>(cache);
        Assert.Null(await cache.GetAsync<string>("k"));         // miss-through.
        await cache.SetAsync("k", "v", new CacheEntryOptions()); // no-op, không ném.
        await cache.RemoveAsync("k");                            // no-op, không ném.
    }

    [Fact]
    public async Task Fail_loud_ports_throw_when_no_adapter()
    {
        using var provider = new ServiceCollection()
            .AddEmailCore().AddStorageCore().AddCacheCore().AddMessagingCore().AddSearchCore()
            .BuildServiceProvider();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IEmailSender>().SendAsync(new EmailMessage("to", "s", "<b/>")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IFileStorage>().SaveAsync(new FileBlob("f", "text/plain", Stream.Null)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IDistributedLock>().AcquireAsync("k", TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IIdempotencyStore>().TryBeginAsync("k", TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IRateLimitStore>().TryAcquireAsync("p", 1, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IEventBusPublisher>().PublishAsync(
                new OutboxMessage { EventType = "x", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<ISearchIndex<SearchDoc>>().IndexAsync(new SearchDoc("1")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<ISearchQuery<SearchDoc>>().SearchAsync(new SearchRequest("q")));
    }

    [Fact]
    public void Adapter_overrides_default_via_replace()
    {
        var services = new ServiceCollection();
        services.AddEmailCore(); // default fail-loud.
        services.Replace(ServiceDescriptor.Singleton<IEmailSender, FakeEmailSender>()); // adapter override.

        using var provider = services.BuildServiceProvider();

        // Replace → đúng 1 registration; adapter thắng (KHÔNG còn throwing default).
        Assert.IsType<FakeEmailSender>(provider.GetRequiredService<IEmailSender>());
        Assert.Single(services, d => d.ServiceType == typeof(IEmailSender));
    }

    [Fact]
    public void External_auth_registry_resolves_by_name_and_fails_loud_for_unknown()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthCore();
        services.AddSingleton<IExternalAuthProvider>(new FakeAuthProvider("google"));
        services.AddSingleton<IExternalAuthProvider>(new FakeAuthProvider("zalo"));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IExternalAuthProviderRegistry>();

        Assert.Equal("google", registry.Resolve("google").Name);
        Assert.Equal("zalo", registry.Resolve("ZALO").Name); // case-insensitive.
        Assert.Throws<InvalidOperationException>(() => registry.Resolve("facebook"));
    }

    [Fact]
    public void Core_registration_is_idempotent()
    {
        // Gọi core 2 lần (TryAdd) → vẫn đúng 1 registration mỗi port (không tạo trùng).
        var services = new ServiceCollection();
        services.AddCacheCore().AddCacheCore();

        Assert.Single(services, d => d.ServiceType == typeof(IAppCache));
    }
}
