using System.Reflection;
using Bedrock.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

// ── Test types cho convention scan / duplicate-guard ────────────────────────
#pragma warning disable CA1040 // Interface marker/port rỗng cho test DI convention — chủ đích.
public interface IScannedThing;

public interface IScannedSingleton;

public interface IDupPort;

public interface IPlainPort; // không marker — test duplicate qua RequiredPort.
#pragma warning restore CA1040

public sealed class ScannedScopedThing : IScannedThing, IScopedService;

public sealed class ScannedSingletonThing : IScannedSingleton, ISingletonService;

/// <summary>Có marker NHƯNG <see cref="IManualRegistration"/> → phải bị LOẠI khỏi auto-scan (F6).</summary>
public sealed class ManuallyRegisteredThing : IScannedThing, IScopedService, IManualRegistration;

/// <summary>Chỉ marker, không interface nghiệp vụ → self-registration.</summary>
public sealed class SelfOnlyThing : ITransientService;

public sealed class DupA : IDupPort, IScopedService;

public sealed class DupB : IDupPort, IScopedService;

public sealed class PlainA : IPlainPort;

public sealed class PlainB : IPlainPort;

/// <summary>Task 10.1 — cỗ máy đăng ký theo convention (marker scan + duplicate-guard).</summary>
public sealed class RegistrationConventionTests
{
    private static readonly Assembly TestAssembly = typeof(ScannedScopedThing).Assembly;

    [Fact]
    public void Scan_registers_marker_types_with_correct_lifetime()
    {
        var services = new ServiceCollection().AddBedrockConventions(TestAssembly);

        var scoped = services.Single(d =>
            d.ServiceType == typeof(IScannedThing) && d.ImplementationType == typeof(ScannedScopedThing));
        Assert.Equal(ServiceLifetime.Scoped, scoped.Lifetime);

        var singleton = services.Single(d =>
            d.ServiceType == typeof(IScannedSingleton) && d.ImplementationType == typeof(ScannedSingletonThing));
        Assert.Equal(ServiceLifetime.Singleton, singleton.Lifetime);
    }

    [Fact]
    public void Scan_excludes_manual_registration_types()
    {
        var services = new ServiceCollection().AddBedrockConventions(TestAssembly);

        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(ManuallyRegisteredThing));
    }

    [Fact]
    public void Scan_self_registers_type_without_business_interface()
    {
        var services = new ServiceCollection().AddBedrockConventions(TestAssembly);

        var self = services.Single(d => d.ServiceType == typeof(SelfOnlyThing));
        Assert.Equal(typeof(SelfOnlyThing), self.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, self.Lifetime);
    }

    [Fact]
    public void DuplicateGuard_throws_for_two_marker_impls_of_single_port()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDupPort, DupA>();
        services.AddScoped<IDupPort, DupB>();

        var ex = Assert.Throws<InvalidOperationException>(() => services.ValidateSingleImplementationPorts());
        Assert.Contains("IDupPort", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateGuard_passes_when_whitelisted_multi_impl()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDupPort, DupA>();
        services.AddScoped<IDupPort, DupB>();
        services.AllowMultipleImplementations<IDupPort>();

        services.ValidateSingleImplementationPorts(); // không ném.
    }

    [Fact]
    public void DuplicateGuard_throws_for_declared_required_port_without_marker()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPlainPort, PlainA>();
        services.AddScoped<IPlainPort, PlainB>();
        services.AddRequiredPort<IPlainPort>();

        var ex = Assert.Throws<InvalidOperationException>(() => services.ValidateSingleImplementationPorts());
        Assert.Contains("IPlainPort", ex.Message, StringComparison.Ordinal);
    }
}
