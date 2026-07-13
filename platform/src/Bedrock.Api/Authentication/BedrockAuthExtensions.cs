using Bedrock.Api.ErrorHandling;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bedrock.Api.Authentication;

/// <summary>
/// Đăng ký CƠ CHẾ xác thực (KHÔNG policy nghiệp vụ — F3): JWT bearer verify theo key-ring (<see cref="JwtKeyRingOptions"/>,
/// F22), bind <see cref="ICurrentUser"/> từ claims, và trả 401/403 dưới dạng ProblemDetails (F20). Role/permission
/// cụ thể do module Identity/Host khai bằng authorization policy riêng — base KHÔNG khai Admin/Staff.
/// </summary>
public static class BedrockAuthExtensions
{
    public static IServiceCollection AddBedrockAuthCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // JwtBearer verify dùng JwtKeyRingOptions CHIA SẺ (A-18). Đăng ký binding + concrete IDEMPOTENT: chỉ MỘT
        // lần dù cả AuthCore (verify) lẫn AddBedrockSecurity (sign) cùng gọi → tránh double-bind list Keys (config
        // binder APPEND → trùng kid). TryAdd không đủ (AddOptions().Configure luôn THÊM 1 IConfigureOptions → bind
        // 2 lần) nên guard theo sự hiện diện của concrete JwtKeyRingOptions. Bind LAZY (thấy config nạp muộn — F35);
        // KHÔNG bind eager (secret chưa nạp lúc registration). Standalone Bedrock.Api (verify-only) vẫn resolve được.
        if (services.All(descriptor => descriptor.ServiceType != typeof(JwtKeyRingOptions)))
        {
            services.AddOptions<JwtKeyRingOptions>()
                .Configure(options => configuration.GetSection(JwtKeyRingOptions.SectionName).Bind(options));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtKeyRingOptions>>().Value);
        }

        // Verify-only hosts must fail at startup too; invalid key material must never wait for first request.
        services.AddOptions<JwtKeyRingOptions>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<JwtKeyRingOptions>, JwtKeyRingOptionsValidator>());

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtKeyRingOptions>((options, sharedKeyRing) =>
            {
                options.MapInboundClaims = false;   // giữ claim JWT-native ("sub"/"role"/...) — AD-023
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = sharedKeyRing.Issuer,
                    ValidateAudience = true,
                    ValidAudience = sharedKeyRing.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    IssuerSigningKeyResolver = (_, _, kid, _) => ResolveKeys(sharedKeyRing, kid),
                };
                options.Events = BuildProblemDetailsEvents();
            });

        services.AddAuthorization();
        return services;
    }

    private static IEnumerable<SecurityKey> ResolveKeys(JwtKeyRingOptions keyRing, string? kid) =>
        string.IsNullOrWhiteSpace(kid)
            ? []
            : [.. keyRing.Keys
            .Where(k => string.Equals(k.Kid, kid, StringComparison.Ordinal))
            .Select(k => (SecurityKey)new SymmetricSecurityKey(Convert.FromBase64String(k.Secret)) { KeyId = k.Kid })];

    private static JwtBearerEvents BuildProblemDetailsEvents() => new()
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();   // chặn WWW-Authenticate body mặc định, tự ghi ProblemDetails
            await ProblemDetailsWriter.WriteAsync(context.HttpContext, CommonErrors.Unauthorized()).ConfigureAwait(false);
        },
        OnForbidden = context => ProblemDetailsWriter.WriteAsync(context.HttpContext, CommonErrors.Forbidden()),
    };
}
