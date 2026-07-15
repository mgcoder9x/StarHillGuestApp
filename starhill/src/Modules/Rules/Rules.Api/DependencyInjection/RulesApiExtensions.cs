using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Rules.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module Rules (DV-013): đăng ký endpoint module dưới <see cref="IEndpointModule"/> để Host
/// <c>UseBedrockApi</c> resolve + map. Tách khỏi nửa-Infra (<c>AddRulesInfrastructure</c>) để giữ Api⊥Infra (I7).
/// D-Rules.4c-1: admin (RulesAdminEndpointModule). Guest (RulesGuestEndpointModule) thêm ở 4c-api-2.
/// </summary>
public static class RulesApiExtensions
{
    public static IServiceCollection AddRulesApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Append (không TryAdd): mỗi module đóng góp IEndpointModule vào tập resolve IEnumerable.
        services.AddSingleton<IEndpointModule, RulesAdminEndpointModule>();
        return services;
    }
}
