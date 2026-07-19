using Microsoft.Extensions.DependencyInjection;

namespace ModuleName.Infrastructure.DependencyInjection;

/// <summary>Composition seam for ModuleName persistence and infrastructure.</summary>
public static class ModuleNameInfrastructureExtensions
{
    public static IServiceCollection AddModuleNameInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
