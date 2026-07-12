using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Infrastructure.Cryptography;
using Bedrock.Infrastructure.Time;
using Bedrock.Infrastructure.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bedrock.Infrastructure.DependencyInjection;

/// <summary>
/// Wire các port BẢO MẬT (phía cơ chế, không nghiệp vụ): băm mật khẩu (Argon2id), sinh token (CSPRNG), và ký
/// JWT theo key-ring (F22). Đây là các port BẮT BUỘC (§5.5) — đăng ký impl THẬT, KHÔNG default no-op (fail-secure).
/// <para>
/// JWT: ký ở đây, verify ở <c>Bedrock.Api</c>; hai bên gặp nhau ở CONFIG (<see cref="JwtKeyRingOptions"/> section
/// "Jwt"), KHÔNG qua project reference → giữ F14 (AD-008). Key-ring validate-on-start (F35) — sai cấu hình chặn boot.
/// </para>
/// </summary>
public static class BedrockSecurityExtensions
{
    public static IServiceCollection AddBedrockSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<IClock, SystemClock>();

        // Argon2id options (OWASP defaults; tune per prod). Stateless → hasher singleton.
        var hashingOptions = new PasswordHashingOptions();
        configuration.GetSection(PasswordHashingOptions.SectionName).Bind(hashingOptions);
        services.TryAddSingleton(hashingOptions);
        services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        services.TryAddSingleton<ITokenGenerator, CryptoTokenGenerator>();

        // JWT key-ring: VALIDATE-ON-START (design §9.4/F35) — validate lúc host START (sau Build), KHÔNG lúc
        // đăng ký, để config nạp muộn (User-Secrets/env/test) được thấy mà vẫn fail-fast chặn boot khi sai.
        // Bind LAZY qua Configure (đọc config sống ở thời điểm build options) — tránh đọc eager pre-Build.
        services.AddOptions<JwtKeyRingOptions>()
            .Configure(options => configuration.GetSection(JwtKeyRingOptions.SectionName).Bind(options))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<JwtKeyRingOptions>, JwtKeyRingOptionsValidator>());
        // Consumer (JwtTokenService) inject JwtKeyRingOptions trực tiếp → lấy từ options đã bind/validate.
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<JwtKeyRingOptions>>().Value);
        services.TryAddSingleton<IJwtTokenService, JwtTokenService>();

        // Khai 3 port bảo mật là BẮT BUỘC → RequiredPortsValidator chặn boot nếu thiếu (fail-secure §5.5 + F7).
        services.BedrockStartupValidation()
            .RequirePort(typeof(IPasswordHasher))
            .RequirePort(typeof(ITokenGenerator))
            .RequirePort(typeof(IJwtTokenService));

        return services;
    }
}
