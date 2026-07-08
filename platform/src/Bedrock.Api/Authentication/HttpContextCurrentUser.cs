using System.Security.Claims;
using Bedrock.Application.Ports.Users;
using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.Authentication;

/// <summary>
/// Impl <see cref="ICurrentUser"/> đọc từ <see cref="ClaimsPrincipal"/> của HTTP request (F23). Đây là CƠ CHẾ
/// (Api): đọc claim chuẩn — KHÔNG hardcode role/permission nghiệp vụ nào (F3). Không có ngữ cảnh đăng nhập
/// → trả null/empty, KHÔNG ném. Claim names là JWT-native (MapInboundClaims=false ở JwtBearer — AD-023).
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    // Tên claim JWT-native (không map sang URI dài của .NET).
    private const string SubClaim = "sub";
    private const string RoleClaim = "role";
    private const string PermissionClaim = "permission";
    private const string TenantClaim = "tenant_id";
    private const string SessionClaim = "sid";

    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(SubClaim)?.Value, out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        [.. Principal?.FindAll(RoleClaim).Select(c => c.Value) ?? []];

    public IReadOnlyCollection<string> Permissions =>
        [.. Principal?.FindAll(PermissionClaim).Select(c => c.Value) ?? []];

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirst(TenantClaim)?.Value, out var id) ? id : null;

    public Guid? SessionId =>
        Guid.TryParse(Principal?.FindFirst(SessionClaim)?.Value, out var id) ? id : null;

    public bool IsInRole(string role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Principal?.FindAll(RoleClaim).Any(c => string.Equals(c.Value, role, StringComparison.Ordinal)) ?? false;
    }

    public bool HasPermission(string permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        return Principal?.FindAll(PermissionClaim).Any(c => string.Equals(c.Value, permission, StringComparison.Ordinal)) ?? false;
    }
}
