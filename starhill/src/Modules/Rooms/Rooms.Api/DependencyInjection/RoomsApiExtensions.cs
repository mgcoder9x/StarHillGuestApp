using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Rooms.Api.DependencyInjection;

/// <summary>
/// Nửa-Api của composition module Rooms (DV-013): đăng ký <see cref="RoomsEndpointModule"/> vào DI dưới
/// <see cref="IEndpointModule"/> để Host <c>UseBedrockApi</c> resolve và map. Tách khỏi nửa-Infra
/// (<c>AddRoomsInfrastructure</c>) để giữ Api⊥Infra (I7); Host gọi cả hai nửa.
/// </summary>
public static class RoomsApiExtensions
{
    public static IServiceCollection AddRoomsApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Append (không TryAdd): mỗi module đóng góp MỘT IEndpointModule vào tập resolve IEnumerable.
        services.AddSingleton<IEndpointModule, RoomsEndpointModule>();
        return services;
    }
}
