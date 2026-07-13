using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace ResortConfig.Api.DependencyInjection;

/// <summary>
/// Nửa-Api của composition module ResortConfig (DV-013): đăng ký <see cref="ResortConfigEndpointModule"/> dưới
/// <see cref="IEndpointModule"/> để Host <c>UseBedrockApi</c> resolve và map. Tách khỏi nửa-Infra
/// (<c>AddResortConfigInfrastructure</c>) để giữ Api⊥Infra (I7); Host gọi cả hai nửa.
/// </summary>
public static class ResortConfigApiExtensions
{
    public static IServiceCollection AddResortConfigApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Append (không TryAdd): mỗi module đóng góp MỘT IEndpointModule vào tập resolve IEnumerable.
        services.AddSingleton<IEndpointModule, ResortConfigEndpointModule>();
        return services;
    }
}
