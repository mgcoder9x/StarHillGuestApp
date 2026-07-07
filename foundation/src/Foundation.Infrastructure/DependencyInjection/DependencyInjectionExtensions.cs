using System.Reflection;
using FluentValidation;
using Foundation.SharedKernel.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Foundation.Infrastructure.DependencyInjection;

/// <summary>
/// Đăng ký service theo CONVENTION (Scrutor + marker interface) — thay AutoDependency tự viết của reference.
/// Req 2: marker → lifetime (2.1); chỉ quét assembly khai báo tường minh (2.2); interface 0 impl → bỏ qua (2.3);
/// nhiều impl → đăng ký hết (2.5); class có ≥2 marker lifetime khác nhau → FAIL khởi động (2.6).
/// </summary>
public static class DependencyInjectionExtensions
{
    private static readonly Type[] LifetimeMarkers =
    [
        typeof(IScopedService),
        typeof(ISingletonService),
        typeof(ITransientService),
    ];

    /// <summary>Quét mặc định assembly Application + Infrastructure (qua AssemblyMarker tường minh).</summary>
    public static IServiceCollection AddFoundationServices(this IServiceCollection services) =>
        services.AddFoundationServices(
            typeof(Application.AssemblyMarker).Assembly,
            typeof(AssemblyMarker).Assembly);

    public static IServiceCollection AddFoundationServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        GuardAgainstLifetimeConflicts(assemblies);

        // Persistence NỀN (EfUnitOfWork/EfRefreshTokenStore...) bị LOẠI khỏi auto-scan: chúng hard-require
        // một FoundationDbContext cụ thể mà base KHÔNG cung cấp. Nếu auto-đăng ký, thiếu DbContext sẽ lỗi
        // lúc CHẠY (per-request) thay vì fail-fast lúc khởi động → khó chẩn đoán. Vì vậy persistence được
        // wire TƯỜNG MINH qua AddFoundationPersistence<TContext>() (một đăng ký duy nhất, rõ phụ thuộc).
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
                .AddClasses(c => c.AssignableTo<IScopedService>().NotInNamespaceOf<Persistence.FoundationDbContext>(), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(c => c.AssignableTo<ISingletonService>().NotInNamespaceOf<Persistence.FoundationDbContext>(), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
                .AddClasses(c => c.AssignableTo<ITransientService>().NotInNamespaceOf<Persistence.FoundationDbContext>(), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

        // Bọc validation quanh mọi IUseCase<TIn,TOut> + ICommandUseCase<TIn> (Req 12.1/12.2). TryDecorate: không ném nếu chưa có.
        services.TryDecorate(typeof(Application.Common.IUseCase<,>), typeof(Application.Common.ValidationUseCaseDecorator<,>));
        services.TryDecorate(typeof(Application.Common.ICommandUseCase<>), typeof(Application.Common.ValidationCommandUseCaseDecorator<>));

        // Đăng ký validator từ CHÍNH các assembly được quét (Req 12.1) — để decorator nhận IValidator<TIn> thật.
        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        return services;
    }

    /// <summary>Req 2.6: một class hiện thực nhiều marker lifetime khác nhau là mập mờ → chặn ngay (fail-fast).</summary>
    private static void GuardAgainstLifetimeConflicts(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                var markerCount = LifetimeMarkers.Count(marker => marker.IsAssignableFrom(type));
                if (markerCount > 1)
                {
                    throw new InvalidOperationException(
                        $"Type '{type.FullName}' hiện thực nhiều marker lifetime (IScopedService/ISingletonService/ITransientService) — xung đột lifetime, không đăng ký mập mờ.");
                }
            }
        }
    }
}
