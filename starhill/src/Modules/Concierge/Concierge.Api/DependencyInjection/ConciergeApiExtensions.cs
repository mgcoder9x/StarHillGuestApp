using Bedrock.Api.Endpoints;
using Concierge.Application;
using Concierge.Api.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace Concierge.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module Concierge: đăng ký endpoint module dưới <see cref="IEndpointModule"/> để Host
/// <c>UseBedrockApi</c> resolve + map. Tách khỏi nửa-Infra (<c>AddConciergeInfrastructure</c>) để giữ Api⊥Infra (I7).
/// K-Con.3: admin (board/detail/reply/read/close + notes CRUD) + guest (gửi tin + đọc hội thoại). K-Con.4: realtime
/// SignalR — <c>AddSignalR</c> + map <see cref="ChatHub"/> (/hubs/chat) + OVERRIDE <c>NoOpConciergeRealtimeNotifier</c>
/// (Infrastructure) bằng <see cref="SignalRConciergeNotifier"/> (last-registration-wins; Host gọi AddConciergeApi SAU
/// AddConciergeInfrastructure). Mirror AddHousekeepingApi/AddFaqApi.
/// </summary>
public static class ConciergeApiExtensions
{
    public static IServiceCollection AddConciergeApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointModule, ConciergeAdminEndpointModule>();
        services.AddSingleton<IEndpointModule, ConciergeGuestEndpointModule>();

        // K-Con.4 realtime: SignalR + hub map + auth-on-join helper + notifier THẬT (override no-op).
        services.AddSignalR();
        services.AddSingleton<IEndpointModule, ConciergeHubEndpointModule>();
        services.AddScoped<ConciergeHubAuthorizer>();
        // OVERRIDE port realtime: use case (đăng ký ở Infrastructure) sẽ resolve bản SignalR này (đăng ký sau → thắng).
        services.AddSingleton<IConciergeRealtimeNotifier, SignalRConciergeNotifier>();
        return services;
    }
}
