using Bedrock.Infrastructure.Cryptography;
using Bedrock.Infrastructure.Persistence.Messaging;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// A-16 guard LOCAL: mọi options family Infrastructure có validator fail-fast (wired `.Validate(IsValid).ValidateOnStart()`
/// hoặc eager `Validate` lúc registration). Test này khoá LOGIC validator (pure) — cấu hình sai bị từ chối; cấu hình
/// hợp lệ pass. Việc validator được GỌI lúc boot đã verify V0 ở extension (OutboxDispatcherExtensions/BedrockSecurityExtensions).
/// </summary>
public sealed class OptionsValidationTests
{
    // ── PasswordHashingOptions (A-16 + nền A-17) ──
    [Fact]
    public void PasswordHashing_defaults_are_valid()
    {
        PasswordHashingOptions.Validate(new PasswordHashingOptions()); // OWASP defaults — không ném.
    }

    [Theory]
    [InlineData(1024, 2, 1, 16, 32)]      // memory < 8 MiB
    [InlineData(19456, 0, 1, 16, 32)]     // iterations < 1
    [InlineData(19456, 21, 1, 16, 32)]    // iterations > 20
    [InlineData(19456, 2, 33, 16, 32)]    // parallelism > 32
    [InlineData(19456, 2, 1, 8, 32)]      // salt < 16
    [InlineData(19456, 2, 1, 16, 16)]     // hash < 32
    [InlineData(2000000, 2, 1, 16, 32)]   // memory > 1 GiB
    public void PasswordHashing_out_of_bounds_fails(int mem, int iter, int par, int salt, int hash)
    {
        var options = new PasswordHashingOptions
        {
            MemoryKib = mem,
            Iterations = iter,
            DegreeOfParallelism = par,
            SaltSize = salt,
            HashSize = hash,
        };
        Assert.Throws<InvalidOperationException>(() => PasswordHashingOptions.Validate(options));
    }

    // ── OutboxDispatcherOptions ──
    [Fact]
    public void OutboxDispatcher_defaults_are_valid() =>
        Assert.True(OutboxDispatcherOptions.IsValid(new OutboxDispatcherOptions()));

    [Fact]
    public void OutboxDispatcher_invalid_values_fail()
    {
        Assert.False(OutboxDispatcherOptions.IsValid(new OutboxDispatcherOptions { BatchSize = 0 }));
        Assert.False(OutboxDispatcherOptions.IsValid(new OutboxDispatcherOptions { MaxAttempts = 0 }));
        Assert.False(OutboxDispatcherOptions.IsValid(new OutboxDispatcherOptions { BaseDelay = TimeSpan.Zero }));
        Assert.False(OutboxDispatcherOptions.IsValid(
            new OutboxDispatcherOptions { BaseDelay = TimeSpan.FromMinutes(10), MaxDelay = TimeSpan.FromMinutes(1) })); // MaxDelay < BaseDelay
        Assert.False(OutboxDispatcherOptions.IsValid(new OutboxDispatcherOptions { ClaimLease = TimeSpan.Zero }));
    }

    // ── OutboxDispatcherWorkerOptions ──
    [Fact]
    public void OutboxWorker_poll_interval_must_be_positive()
    {
        Assert.True(OutboxDispatcherWorkerOptions.IsValid(new OutboxDispatcherWorkerOptions()));
        Assert.False(OutboxDispatcherWorkerOptions.IsValid(new OutboxDispatcherWorkerOptions { PollInterval = TimeSpan.Zero }));
    }

    // ── OutboxRetentionOptions ──
    [Fact]
    public void OutboxRetention_ttl_rules()
    {
        Assert.True(OutboxRetentionOptions.IsValid(new OutboxRetentionOptions())); // ProcessedRetention 7d, DeadLetter null
        Assert.True(OutboxRetentionOptions.IsValid(new OutboxRetentionOptions { DeadLetterRetention = TimeSpan.FromDays(7) }));
        Assert.False(OutboxRetentionOptions.IsValid(new OutboxRetentionOptions { ProcessedRetention = TimeSpan.Zero }));
        Assert.False(OutboxRetentionOptions.IsValid(new OutboxRetentionOptions { DeadLetterRetention = TimeSpan.Zero }));
    }
}
