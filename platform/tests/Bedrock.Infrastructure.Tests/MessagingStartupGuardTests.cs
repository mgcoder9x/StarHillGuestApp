using Bedrock.Application.DependencyInjection;
using Bedrock.Infrastructure.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// GUARD P1-15 — startup fail-fast khi có outbox PRODUCER nhưng KHÔNG có DISPATCHER WORKER (event sẽ tích lũy im
/// lặng, không bao giờ phát). Chặn boot trừ khi khai OFFLINE tường minh. Thuần (dựng
/// <see cref="RequiredPortsValidator"/> trực tiếp + <see cref="StartupValidationOptions"/>) → KHÔNG cần Docker.
/// Chốt: bất biến "producer ⇒ phải có drainer (hoặc offline có ý thức)" do startup guard giữ, không âm thầm.
/// </summary>
public sealed class MessagingStartupGuardTests
{
    private sealed class FakeContext;

    private static RequiredPortsValidator Validator(
        StartupValidationOptions options,
        IConfiguration? configuration = null)
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        return new RequiredPortsValidator(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            configuration ?? new ConfigurationBuilder().Build());
    }

    [Fact]
    public async Task Outbox_producer_without_dispatcher_blocks_boot()
    {
        var options = new StartupValidationOptions();
        options.RegisterOutboxProducer(typeof(FakeContext));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Validator(options).StartAsync(default));

        Assert.Contains("tích lũy im lặng", ex.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(FakeContext).FullName!, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Outbox_producer_with_dispatcher_boots()
    {
        var options = new StartupValidationOptions();
        options.RegisterOutboxProducer(typeof(FakeContext));
        options.RegisterOutboxDrainer(typeof(FakeContext));

        await Validator(options).StartAsync(default); // không ném — producer đã có drainer.
    }

    [Fact]
    public async Task Explicit_allow_offline_permits_producer_without_dispatcher()
    {
        var options = new StartupValidationOptions();
        options.RegisterOutboxProducer(typeof(FakeContext));
        options.AllowOutboxWithoutDispatcher();

        await Validator(options).StartAsync(default); // không ném — offline khai TƯỜNG MINH.
    }

    [Fact]
    public async Task Config_allow_offline_permits_producer_without_dispatcher()
    {
        // Escape hatch qua CONFIG (đường smoke test/env dùng — đọc lúc StartAsync, post-build).
        var options = new StartupValidationOptions();
        options.RegisterOutboxProducer(typeof(FakeContext));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RequiredPortsValidator.AllowOutboxWithoutDispatcherKey] = "true",
            })
            .Build();

        await Validator(options, config).StartAsync(default); // không ném — offline khai qua config.
    }

    [Fact]
    public async Task No_outbox_producer_boots()
    {
        await Validator(new StartupValidationOptions()).StartAsync(default); // không producer → không ràng buộc.
    }
}
