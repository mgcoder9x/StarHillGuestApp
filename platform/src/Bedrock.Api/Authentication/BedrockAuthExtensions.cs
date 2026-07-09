using Bedrock.Api.ErrorHandling;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // Bind LOCAL (chỉ để cấu hình JwtBearer: Issuer/Audience + resolver key). KHÔNG đăng ký làm singleton
        // JwtKeyRingOptions: singleton concrete đến từ AddBedrockSecurity qua IOptions (validate-on-start, thấy
        // config nạp muộn — F35/task 19). ResolveKeys chạy LAZY lúc verify token (không đụng lúc boot).
        var keyRing = new JwtKeyRingOptions();
        configuration.GetSection(JwtKeyRingOptions.SectionName).Bind(keyRing);

        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;   // giữ claim JWT-native ("sub"/"role"/...) — AD-023
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keyRing.Issuer,
                    ValidateAudience = true,
                    ValidAudience = keyRing.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    IssuerSigningKeyResolver = (_, _, kid, _) => ResolveKeys(keyRing, kid),
                };
                options.Events = BuildProblemDetailsEvents();
            });

        services.AddAuthorization();
        return services;
    }

    private static IEnumerable<SecurityKey> ResolveKeys(JwtKeyRingOptions keyRing, string? kid) =>
        [.. keyRing.Keys
            .Where(k => string.IsNullOrEmpty(kid) || string.Equals(k.Kid, kid, StringComparison.Ordinal))
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
