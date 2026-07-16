using System.Security.Claims;
using Identity.Domain;

namespace Identity.Application;

/// <summary>
/// Dựng <see cref="ClaimsIdentity"/> cho access-token: <c>sub</c> = UserId + <c>role</c> = tên enum viết thường
/// ("admin"/"staff", claim JWT-native khớp base RoleClaimType="role"). NGUỒN DUY NHẤT dựng identity access-token —
/// DÙNG CHUNG login (F.1b) + refresh-rotation (F.2) → KHÔNG lệch claim giữa hai đường (login cấp role thì refresh
/// cũng phải cấp role, nếu không admin mất quyền sau refresh). Hợp đồng enum↔policy (StarHillPolicies) được GUARD
/// bởi <c>RoleClaimContractTests</c> (KHÔNG ref StarHill.Authorization vào Application — FrameworkReference ASP.NET
/// sẽ phá I7).
/// </summary>
internal static class IdentityClaims
{
    public static ClaimsIdentity Build(Guid userId, UserRole role) =>
        new(
            [new Claim("sub", userId.ToString()), new Claim("role", role.ToString().ToLowerInvariant())],
            authenticationType: "jwt");
}
