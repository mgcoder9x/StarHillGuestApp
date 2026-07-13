using Microsoft.Extensions.DependencyInjection;

namespace StarHill.Authorization;

/// <summary>
/// Đăng ký policy Admin/Staff của sản phẩm (QR-AD-020). Host gọi MỘT lần sau <c>AddBedrockApi</c>
/// (<c>AddBedrockAuthCore</c> đã gọi <c>AddAuthorization()</c> nên thêm named policy ở đây là ADDITIVE, an toàn).
/// Superset: <see cref="StarHillPolicies.RequireStaff"/> = RequireRole(staff, admin) → Admin thỏa cả policy Staff.
/// </summary>
public static class StarHillAuthorizationExtensions
{
    public static IServiceCollection AddStarHillAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorizationBuilder()
            .AddPolicy(StarHillPolicies.RequireAdmin, policy =>
                policy.RequireRole(StarHillPolicies.RoleAdmin))
            .AddPolicy(StarHillPolicies.RequireStaff, policy =>
                policy.RequireRole(StarHillPolicies.RoleStaff, StarHillPolicies.RoleAdmin));

        return services;
    }
}
