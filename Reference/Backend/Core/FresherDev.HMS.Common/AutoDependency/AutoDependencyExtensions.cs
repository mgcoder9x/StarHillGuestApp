using System.Reflection;
using FresherDev.HMS.Common.AutoDependency;
using Microsoft.Extensions.DependencyInjection;

namespace FresherDev.HMS.Common;

public static class AutoDependencyExtensions
{
    public static void AddAutoDependency(this IServiceCollection services)
    {
        var projectTypes = AssemblyHelper.GetProjectTypes();


        var asemblyInterfaces = AssemblyInterface.GetValidInterfaces(projectTypes);

        Console.WriteLine(">> Add Dependency:");
        Console.ForegroundColor = ConsoleColor.Green;
        foreach (var @interface in asemblyInterfaces)
        {
            // Lấy ra toàn bộ các class implement từ @interface
            var implementTypes = AssemblyImplement.GetImplementTypes(@interface, projectTypes);
            if (!implementTypes.Any() || implementTypes.Count > 1)
            {
                throw new Exception($"[{@interface.FullName}] have not implement type or more than one");
            }

            var dependencyType = services.AddDependency(@interface, implementTypes.First());
            Console.WriteLine($"{@interface.Name} => {implementTypes.First().Name}: {dependencyType}");
        }

        Console.ResetColor();
        Console.WriteLine("==============================================");
    }

    private static DependencyType AddDependency(this IServiceCollection services, Type @interface, Type implementType)
    {
        var dependencyType = GetDependencyType(@interface);

        switch (dependencyType)
        {
            case DependencyType.Singleton:
                {
                    services.AddSingleton(@interface, implementType);
                    return dependencyType;
                }

            case DependencyType.Transient:
                {
                    services.AddTransient(@interface, implementType);
                    return dependencyType;
                }

            default:
                {
                    services.AddScoped(@interface, implementType);
                    return dependencyType;
                }
        }
    }

    /// <summary>
    /// Lấy ra DependencyType dựa vào attribute của interface.
    /// Mặc định nếu không tồn tại Attribute thì sẽ trả về Transient
    /// </summary>
    private static DependencyType GetDependencyType(Type interfaceType)
    {
        var autoDependencyAttribute = interfaceType.GetCustomAttribute<AutoDependencyAttribute>();
        if (autoDependencyAttribute == null)
        {
            return DependencyType.Transient;
        }

        return autoDependencyAttribute.Type;
    }
}
