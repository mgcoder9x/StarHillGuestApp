using System.Security.Claims;
using ResortQr.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ResortQr.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> đọc từ <see cref="ClaimsPrincipal"/> của request (JWT đã validate).
/// Claim: <c>sub</c> = userId, <c>role</c> = vai trò. Không có ngữ cảnh → trả null/rỗng, KHÔNG ném.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("role").Select(c => c.Value).ToArray() ?? [];

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
