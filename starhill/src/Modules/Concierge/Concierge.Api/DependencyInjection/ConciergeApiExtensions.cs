using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Concierge.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module Concierge (K-Con.3): đăng ký endpoint module dưới <see cref="IEndpointModule"/> để Host
/// <c>UseBedrockApi</c> resolve + map. Tách khỏi nửa-Infra (<c>AddConciergeInfrastructure</c>) để giữ Api⊥Infra (I7).
/// Admin (board/detail/reply/read/close + notes CRUD) + guest (gửi tin + đọc hội thoại). Mirror AddHousekeepingApi/AddFaqApi.
/// SignalR ChatHub + notifier override sẽ thêm ở K-Con.4 (Host MapHub).
/// </summary>
public static class ConciergeApiExtensions
{
    public static IServiceCollection AddConciergeApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointModule, ConciergeAdminEndpointModule>();
        services.AddSingleton<IEndpointModule, ConciergeGuestEndpointModule>();
        return services;
    }
}
