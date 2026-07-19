using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace ModuleName.Api.DependencyInjection;

/// <summary>Composition seam for ModuleName endpoints.</summary>
public static class ModuleNameApiExtensions
{
    public static IServiceCollection AddModuleNameApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IEndpointModule, ModuleNameEndpointModule>();
        return services;
    }
}
