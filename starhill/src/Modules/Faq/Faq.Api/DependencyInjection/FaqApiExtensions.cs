using Bedrock.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Faq.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module Faq (E-Faq.4): đăng ký endpoint module dưới <see cref="IEndpointModule"/> để Host
/// <c>UseBedrockApi</c> resolve + map. Tách khỏi nửa-Infra (<c>AddFaqInfrastructure</c>) để giữ Api⊥Infra (I7).
/// Admin (FaqAdminEndpointModule — CRUD/reorder) + guest (FaqGuestEndpointModule — đọc cây FAQ). Mirror AddRulesApi.
/// </summary>
public static class FaqApiExtensions
{
    public static IServiceCollection AddFaqApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointModule, FaqAdminEndpointModule>();
        services.AddSingleton<IEndpointModule, FaqGuestEndpointModule>();
        return services;
    }
}
