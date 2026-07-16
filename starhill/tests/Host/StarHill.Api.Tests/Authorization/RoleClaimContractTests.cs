using Identity.Domain;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// GUARD hợp đồng ROLE-CLAIM (F.1b — chống drift enum↔policy). <c>LoginUseCase</c> DERIVE claim <c>role</c> =
/// <c>UserRole.ToString().ToLowerInvariant()</c> (CỐ Ý không ref StarHill.Authorization vào Identity.Application vì
/// project đó FrameworkReference ASP.NET → phá I7). Test này gắn hai đầu hợp đồng lại: nếu ai đổi tên enum
/// <see cref="UserRole"/> HOẶC đổi hằng <see cref="StarHillPolicies"/> làm chúng lệch nhau → FAIL BUILD (login
/// phát role sai → policy RequireAdmin/RequireStaff từ chối âm thầm 403). Đây là nơi bắt drift đó.
/// </summary>
public sealed class RoleClaimContractTests
{
    [Fact]
    public void Admin_enum_name_maps_to_policy_role_admin()
    {
        Assert.Equal(StarHillPolicies.RoleAdmin, UserRole.Admin.ToString().ToLowerInvariant());
    }

    [Fact]
    public void Staff_enum_name_maps_to_policy_role_staff()
    {
        Assert.Equal(StarHillPolicies.RoleStaff, UserRole.Staff.ToString().ToLowerInvariant());
    }
}
