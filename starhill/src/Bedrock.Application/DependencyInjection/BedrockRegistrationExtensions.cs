using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Application.DependencyInjection;

/// <summary>
/// Cỗ máy đăng ký DI theo convention của Bedrock (F6/F18): auto-scan theo marker, khai port bắt buộc, và
/// duplicate-guard cho port single-impl. Scan bằng reflection THUẦN (KHÔNG Scrutor cho marker-scan — AD-033): đủ đơn
/// giản để tự sở hữu + test, giữ lõi Application tối thiểu dependency. Mọi API scan nhận <c>params Assembly[]</c> (F6).
/// (Scrutor CHỈ được dùng cho decorator pipeline ở <see cref="BedrockCoreExtensions"/> — AD-037; marker-scan này giữ nguyên.)
/// </summary>
public static class BedrockRegistrationExtensions
{
    private static readonly Type[] MarkerInterfaces =
        [typeof(IScopedService), typeof(ISingletonService), typeof(ITransientService)];

    /// <summary>Lấy (hoặc tạo) sổ <see cref="StartupValidationOptions"/> singleton trong collection.</summary>
    public static StartupValidationOptions BedrockStartupValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(StartupValidationOptions)
                && descriptor.ImplementationInstance is StartupValidationOptions existing)
            {
                return existing;
            }
        }

        var created = new StartupValidationOptions();
        services.AddSingleton(created);
        return created;
    }

    /// <summary>Khai một port BẮT BUỘC (thiếu implementation lúc boot → <c>RequiredPortsValidator</c> chặn boot).</summary>
    public static IServiceCollection AddRequiredPort<TPort>(this IServiceCollection services)
        where TPort : class
    {
        services.BedrockStartupValidation().RequirePort(typeof(TPort));
        return services;
    }

    /// <summary>Khai một port CỐ Ý đa-implementation (duplicate-guard bỏ qua).</summary>
    public static IServiceCollection AllowMultipleImplementations<TPort>(this IServiceCollection services)
        where TPort : class
    {
        services.BedrockStartupValidation().AllowMultipleImplementations(typeof(TPort));
        return services;
    }

    /// <summary>
    /// Auto-scan các assembly ứng dụng: class non-abstract implement marker
    /// (<see cref="IScopedService"/>/<see cref="ISingletonService"/>/<see cref="ITransientService"/>) được đăng ký
    /// (append) theo lifetime tương ứng cho MỌI interface nghiệp vụ nó implement (bỏ marker + interface <c>System.*</c>);
    /// không có interface phù hợp → đăng ký chính nó (self). Class có <see cref="IManualRegistration"/> bị LOẠI (F6).
    /// Implement nhiều marker → ném lỗi (ambiguous). Duplicate single-impl do scan/nguồn khác → bắt bởi
    /// <see cref="ValidateSingleImplementationPorts"/>.
    /// </summary>
    public static IServiceCollection AddBedrockConventions(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract || typeof(IManualRegistration).IsAssignableFrom(type))
                {
                    continue;
                }

                var lifetime = ResolveLifetime(type);
                if (lifetime is null)
                {
                    continue; // không có marker → không thuộc convention.
                }

                var serviceInterfaces = type.GetInterfaces()
                    .Where(i => !MarkerInterfaces.Contains(i))
                    .Where(i => i != typeof(IManualRegistration))
                    .Where(i => i.Namespace is null || !i.Namespace.StartsWith("System", StringComparison.Ordinal))
                    .ToArray();

                if (serviceInterfaces.Length == 0)
                {
                    services.Add(new ServiceDescriptor(type, type, lifetime.Value)); // self-registration fallback.
                    continue;
                }

                foreach (var serviceInterface in serviceInterfaces)
                {
                    services.Add(new ServiceDescriptor(serviceInterface, type, lifetime.Value));
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Duplicate-guard (F18): port single-impl KHÔNG được có &gt;1 registration (last-wins âm thầm = bug). Chỉ soi
    /// service do convention Bedrock đăng ký (impl implement marker) HOẶC port đã khai <c>RequirePort</c>; bỏ qua
    /// port trong whitelist đa-impl và open-generic. Ném liệt kê GỘP mọi vi phạm (không dừng ở cái đầu). Host gọi
    /// sau khi compose xong (trước Build).
    /// </summary>
    public static void ValidateSingleImplementationPorts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var registry = services.BedrockStartupValidation();
        var violations = new List<string>();

        foreach (var group in services.GroupBy(descriptor => descriptor.ServiceType))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            var serviceType = group.Key;
            if (serviceType.IsGenericTypeDefinition || registry.MultiImplementationPorts.Contains(serviceType))
            {
                continue;
            }

            var guarded = registry.RequiredPorts.Contains(serviceType)
                || group.Any(descriptor => descriptor.ImplementationType is not null && ImplementsMarker(descriptor.ImplementationType));
            if (!guarded)
            {
                continue;
            }

            var implementations = group.Select(DescribeImplementation);
            violations.Add($"{serviceType.FullName} có {group.Count()} đăng ký [{string.Join(", ", implementations)}]");
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate-guard (F18): port single-impl bị đăng ký trùng (nếu CỐ Ý đa-impl → khai "
                + "AllowMultipleImplementations): " + string.Join(" | ", violations));
        }
    }

    private static ServiceLifetime? ResolveLifetime(Type type)
    {
        var markers = MarkerInterfaces.Where(marker => marker.IsAssignableFrom(type)).ToArray();
        if (markers.Length == 0)
        {
            return null;
        }

        if (markers.Length > 1)
        {
            throw new InvalidOperationException(
                $"{type.FullName} implement NHIỀU marker lifetime ({string.Join(", ", markers.Select(m => m.Name))}) — chỉ được một.");
        }

        var marker = markers[0];
        if (marker == typeof(IScopedService))
        {
            return ServiceLifetime.Scoped;
        }

        return marker == typeof(ISingletonService) ? ServiceLifetime.Singleton : ServiceLifetime.Transient;
    }

    private static bool ImplementsMarker(Type implementationType) =>
        MarkerInterfaces.Any(marker => marker.IsAssignableFrom(implementationType));

    private static string DescribeImplementation(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType?.FullName
        ?? (descriptor.ImplementationInstance is not null ? "instance" : "factory");
}
