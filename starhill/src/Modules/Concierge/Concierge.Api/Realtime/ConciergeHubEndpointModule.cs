using Bedrock.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Concierge.Api.Realtime;

/// <summary>
/// Map <see cref="ChatHub"/> tại <c>/hubs/chat</c> qua cơ chế <see cref="IEndpointModule"/> (Host <c>UseBedrockApi</c>
/// gọi trong <c>UseEndpoints</c> — SAU UseAuthentication/UseAuthorization, nên hub nhận principal đã xác thực). Giữ
/// "thêm module = 1 dòng", KHÔNG đụng pipeline base. <c>AddSignalR</c> đăng ký ở <c>AddConciergeApi</c>.
/// </summary>
public sealed class ConciergeHubEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapHub<ChatHub>("/hubs/chat");
    }
}
