using System.Text;
using System.Text.Json;
using ResortQr.Api.ErrorHandling;
using ResortQr.Application.Abstractions;
using ResortQr.Infrastructure.Security;
using ResortQr.SharedKernel.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ResortQr.Api.Security;

/// <summary>
/// Wiring xác thực JWT Bearer + authorization policy + <see cref="ICurrentUser"/> từ HttpContext.
/// Bearer cấu hình LAZY qua <see cref="IOptions{JwtOptions}"/> (bên ký & verify chung nguồn). Lỗi 401/403
/// trả ĐỒNG NHẤT dạng ProblemDetails (code + traceId + application/problem+json) — Req 4.2.
/// </summary>
public static class ResortQrAuthExtensions
{
    public const string RequireAdminPolicy = "RequireAdmin";
    public const string RequireStaffPolicy = "RequireStaff";
    private const string ProblemContentType = "application/problem+json";

    public static IServiceCollection AddResortQrAuth(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                };

                if (!string.IsNullOrEmpty(jwt.SigningKey))
                {
                    parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
                }

                bearer.TokenValidationParameters = parameters;

                // 401 (thiếu/sai token) & 403 (thiếu quyền) trả ProblemDetails đồng nhất thay vì response mặc định rỗng.
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse(); // chặn challenge mặc định (401 rỗng + WWW-Authenticate)
                        return WriteProblemAsync(context.HttpContext, CommonErrors.Unauthorized());
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, CommonErrors.Forbidden()),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(RequireAdminPolicy, policy => policy.RequireRole("Admin"))
            // Admin là superset của Staff: được phép làm mọi endpoint staff-level (RBAC phân cấp).
            .AddPolicy(RequireStaffPolicy, policy => policy.RequireRole("Staff", "Admin"));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    private static async Task WriteProblemAsync(HttpContext httpContext, Error error)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var problem = ProblemDetailsBuilder.Build(error, ProblemDetailsBuilder.TraceId(httpContext));
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        // Truyền contentType để KHÔNG bị WriteAsJsonAsync ép về application/json.
        await httpContext.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, ProblemContentType);
    }
}
