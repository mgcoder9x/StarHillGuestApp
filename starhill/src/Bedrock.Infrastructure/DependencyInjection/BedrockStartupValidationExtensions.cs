using Bedrock.Application.DependencyInjection;
using Bedrock.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.DependencyInjection;

/// <summary>
/// Đăng ký <see cref="RequiredPortsValidator"/> như một <c>IHostedService</c> chạy lúc boot (F7/I9). Host gọi một
/// lần; danh sách port bắt buộc do các <c>AddXxxCore</c> đóng góp vào <see cref="StartupValidationOptions"/>.
/// </summary>
public static class BedrockStartupValidationExtensions
{
    public static IServiceCollection AddBedrockStartupValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.BedrockStartupValidation(); // đảm bảo registry singleton tồn tại để validator inject.
        services.AddHostedService<RequiredPortsValidator>();
        return services;
    }
}
