using ResortQr.Api.Security;
using ResortQr.Application.Identity;
using ResortQr.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ResortQr.Api.Configuration;

/// <summary>
/// Bind + validate strongly-typed Options với **ValidateOnStart** (fail-fast): thiếu/sai cấu hình → app
/// TỪ CHỐI khởi động (an toàn hơn chạy tạm với default — DEC-020). Đăng ký POCO để service nhận giá trị đã validate.
/// </summary>
public static class ResortQrOptionsExtensions
{
    public static IServiceCollection AddResortQrOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PasswordHashingOptions>>().Value);

        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<RefreshTokenOptions>>().Value);

        return services;
    }
}
