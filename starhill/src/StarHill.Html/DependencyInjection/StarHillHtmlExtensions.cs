using Bedrock.Application.Ports.Html;
using Microsoft.Extensions.DependencyInjection;

namespace StarHill.Html.DependencyInjection;

/// <summary>
/// Đăng ký adapter <see cref="IHtmlSanitizer"/> dùng chung của sản phẩm (QR-AD-031). Host gọi khi module tiêu thụ
/// (Rules/Faq) được cắm; RequirePort(IHtmlSanitizer) để boot fail-fast đặt ở Host wiring (D-Rules.4).
/// </summary>
public static class StarHillHtmlExtensions
{
    public static IServiceCollection AddStarHillHtml(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IHtmlSanitizer, GanssHtmlSanitizerAdapter>();
        return services;
    }
}
