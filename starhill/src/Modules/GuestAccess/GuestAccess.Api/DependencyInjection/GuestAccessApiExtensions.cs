using Bedrock.Api.Endpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuestAccess.Api.DependencyInjection;

/// <summary>
/// Nửa-Api composition module GuestAccess (mirror Rooms): đăng ký <see cref="GuestAccessEndpointModule"/> dưới
/// <see cref="IEndpointModule"/> (Host <c>UseBedrockApi</c> resolve + map) + bind <see cref="GuestAccessOptions"/>
/// với ValidateOnStart (fail-fast cấu hình cookie sai). Tách khỏi nửa-Infra để giữ Api⊥Infra (I7).
/// </summary>
public static class GuestAccessApiExtensions
{
    public static IServiceCollection AddGuestAccessApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IEndpointModule, GuestAccessEndpointModule>();

        services.AddOptions<GuestAccessOptions>()
            .Bind(configuration.GetSection(GuestAccessOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.CookieName) && o.CookieName.StartsWith("__Host-", StringComparison.Ordinal),
                "GuestAccess:CookieName phải bắt đầu bằng '__Host-' (browser enforce Secure/Path=/ — QR-AD-025).")
            .Validate(
                o => o.SessionCookieDays is >= 30 and <= 90,
                "GuestAccess:SessionCookieDays phải trong [30,90] (Req 11.2 — cookie thiết bị dài hạn).")
            .ValidateOnStart();

        return services;
    }
}
