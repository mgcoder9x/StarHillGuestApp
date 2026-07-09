using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api.DependencyInjection;

/// <summary>
/// Nửa-Api của composition module Identity (DV-013): đăng ký <see cref="IdentityEndpointModule"/> vào DI dưới
/// <see cref="IEndpointModule"/> để Host <c>MapBedrockApi</c> resolve và map. Tách khỏi nửa-Infra
/// (<c>AddIdentityInfrastructure</c>) để giữ Api⊥Infra (I7); Host gọi cả hai nửa.
/// </summary>
public static class IdentityApiExtensions
{
    public static IServiceCollection AddIdentityApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Append (không TryAdd): mỗi module đóng góp MỘT IEndpointModule vào tập resolve IEnumerable.
        services.AddSingleton<IEndpointModule, IdentityEndpointModule>();
        return services;
    }
}
