using System.Security.Claims;
using Bedrock.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Bedrock.Api.Tests.Authentication;

public sealed class HttpContextCurrentUserTests
{
    private static HttpContextCurrentUser WithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void Should_map_claims_to_current_user()
    {
        var userId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();

        var user = WithClaims(
            new Claim("sub", userId.ToString()),
            new Claim("role", "a"),
            new Claim("role", "b"),
            new Claim("permission", "orders.read"),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("sid", sessionId.ToString()));

        Assert.True(user.IsAuthenticated);
        Assert.Equal(userId, user.UserId);
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal(sessionId, user.SessionId);
        Assert.Equal(["a", "b"], user.Roles);
        Assert.Equal(["orders.read"], user.Permissions);
        Assert.True(user.IsInRole("a"));
        Assert.False(user.IsInRole("z"));
        Assert.True(user.HasPermission("orders.read"));
        Assert.False(user.HasPermission("orders.write"));
    }

    [Fact]
    public void Anonymous_context_should_return_null_and_empty()
    {
        var user = new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = null });

        Assert.False(user.IsAuthenticated);
        Assert.Null(user.UserId);
        Assert.Null(user.TenantId);
        Assert.Null(user.SessionId);
        Assert.Empty(user.Roles);
        Assert.Empty(user.Permissions);
        Assert.False(user.IsInRole("a"));
        Assert.False(user.HasPermission("p"));
    }
}
