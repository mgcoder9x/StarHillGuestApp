using Bedrock.Application.DependencyInjection;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Infrastructure.Cryptography;
using Bedrock.Infrastructure.Time;
using Bedrock.Infrastructure.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // JWT key-ring: bind + validate-on-start (fail-fast F35). TryAddSingleton để idempotent nếu Api đã bind
        // cùng section "Jwt" (AD-008 — chia sẻ qua config, không qua reference).
        var keyRing = new JwtKeyRingOptions();
        configuration.GetSection(JwtKeyRingOptions.SectionName).Bind(keyRing);
        JwtKeyRingValidation.Validate(keyRing);
        services.TryAddSingleton(keyRing);
        services.TryAddSingleton<IJwtTokenService, JwtTokenService>();

        // Khai 3 port bảo mật là BẮT BUỘC → RequiredPortsValidator chặn boot nếu thiếu (fail-secure §5.5 + F7).
        services.BedrockStartupValidation()
            .RequirePort(typeof(IPasswordHasher))
            .RequirePort(typeof(ITokenGenerator))
            .RequirePort(typeof(IJwtTokenService));

        return services;
    }
}
