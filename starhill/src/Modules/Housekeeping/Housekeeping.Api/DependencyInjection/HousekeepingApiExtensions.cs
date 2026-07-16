using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeping.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module Housekeeping (H-Hk.3): đăng ký endpoint module dưới <see cref="IEndpointModule"/> để Host
/// <c>UseBedrockApi</c> resolve + map. Tách khỏi nửa-Infra (<c>AddHousekeepingInfrastructure</c>) để giữ Api⊥Infra (I7).
/// Admin (board/complete/status/create) + guest (tạo yêu cầu + xem trạng thái). Mirror AddFaqApi/AddRulesApi.
/// </summary>
public static class HousekeepingApiExtensions
{
    public static IServiceCollection AddHousekeepingApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointModule, HousekeepingAdminEndpointModule>();
        services.AddSingleton<IEndpointModule, HousekeepingGuestEndpointModule>();
        return services;
    }
}
