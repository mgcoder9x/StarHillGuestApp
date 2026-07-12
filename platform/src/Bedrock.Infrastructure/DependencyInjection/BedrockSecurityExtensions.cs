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
        PasswordHashingOptions.Validate(hashingOptions);
        services.TryAddSingleton(hashingOptions);
        services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        services.TryAddSingleton<ITokenGenerator, CryptoTokenGenerator>();

        // JWT key-ring: VALIDATE-ON-START (design §9.4/F35) — validate lúc host START (sau Build), KHÔNG lúc
        // đăng ký, để config nạp muộn (User-Secrets/env/test) được thấy mà vẫn fail-fast chặn boot khi sai.
        // Binding + concrete IDEMPOTENT: AuthCore (Bedrock.Api, verify) có thể đã đăng ký CÙNG instance (A-18 —
        // sign + verify chia sẻ). Guard theo concrete để KHÔNG double-bind list Keys (append → trùng kid).
        if (services.All(descriptor => descriptor.ServiceType != typeof(JwtKeyRingOptions)))
        {
            services.AddOptions<JwtKeyRingOptions>()
                .Configure(options => configuration.GetSection(JwtKeyRingOptions.SectionName).Bind(options));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtKeyRingOptions>>().Value);
        }

        // Validate-on-start + validator là trách nhiệm SIGN side (Security) — luôn bật (idempotent: ValidateOnStart
        // đánh dấu options đã cấu hình; validator qua TryAddEnumerable). Sai key-ring → chặn boot mọi môi trường.
        services.AddOptions<JwtKeyRingOptions>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<JwtKeyRingOptions>, JwtKeyRingOptionsValidator>());
        services.TryAddSingleton<IJwtTokenService, JwtTokenService>();

        // Khai 3 port bảo mật là BẮT BUỘC → RequiredPortsValidator chặn boot nếu thiếu (fail-secure §5.5 + F7).
        services.BedrockStartupValidation()
            .RequirePort(typeof(IPasswordHasher))
            .RequirePort(typeof(ITokenGenerator))
            .RequirePort(typeof(IJwtTokenService));

        return services;
    }
}
