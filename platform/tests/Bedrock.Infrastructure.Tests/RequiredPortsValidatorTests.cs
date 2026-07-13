using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Security;
using Bedrock.Infrastructure.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

/// <summary>
/// Task 10.2 — <see cref="RequiredPortsValidator"/> (CP9, fail-fast mọi môi trường). Kiểm: (1) thiếu port →
/// ném liệt kê GỘP mọi port thiếu (không dừng ở cái đầu); (2) đủ port → qua; (3) SCOPE-AWARE: port scoped
/// resolve qua scope KHÔNG ném dù <c>ValidateScopes=true</c> (bằng chứng validator tạo scope, không dùng root).
/// </summary>
public sealed class RequiredPortsValidatorTests
{
    [Fact]
    public async Task StartAsync_throws_listing_all_missing_ports()
    {
        var services = new ServiceCollection();
        var registry = services.BedrockStartupValidation();
        registry.RequirePort(typeof(IPasswordHasher)); // thiếu
        registry.RequirePort(typeof(ITokenGenerator)); // thiếu
        registry.RequirePort(typeof(IScannedThing));   // có (đăng ký dưới)
        services.AddScoped<IScannedThing, ScannedScopedThing>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var validator = new RequiredPortsValidator(
            provider.GetRequiredService<IServiceScopeFactory>(), registry, new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.StartAsync(CancellationToken.None));
        Assert.Contains("IPasswordHasher", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ITokenGenerator", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("IScannedThing", exception.Message, StringComparison.Ordinal); // đã có → không liệt kê.
    }

    [Fact]
    public async Task StartAsync_succeeds_when_all_present_including_scoped_port()
    {
        var services = new ServiceCollection();
        var registry = services.BedrockStartupValidation();
        registry.RequirePort(typeof(IScannedThing)); // scoped
        services.AddScoped<IScannedThing, ScannedScopedThing>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var validator = new RequiredPortsValidator(
            provider.GetRequiredService<IServiceScopeFactory>(), registry, new ConfigurationBuilder().Build());

        // Scope-aware: nếu validator resolve port scoped từ ROOT (ValidateScopes=true) sẽ ném → thành công = đã tạo scope.
        await validator.StartAsync(CancellationToken.None);
    }
}
