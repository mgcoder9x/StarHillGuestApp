using GuestAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using ResortConfig.Contracts.Queries;

namespace Concierge.Api.Realtime;

/// <summary>
/// Hub chat realtime Concierge tại <c>/hubs/chat</c> (K-Con.4). <see cref="AllowAnonymousAttribute"/> ở HUB vì KHÁCH
/// kết nối KHÔNG có JWT (định danh bằng cookie phiên <c>__Host-starhill_guest</c> gửi kèm handshake WebSocket cùng-origin);
/// NHÂN VIÊN kết nối kèm JWT (qua query <c>access_token</c> — Host cấu hình OnMessageReceived cho path này). Phân quyền
/// per-method (KHÔNG [Authorize] toàn hub): dùng <see cref="ConciergeHubAuthorizer"/>.
/// <para><b>Bất biến bảo mật</b>: <see cref="JoinConversation"/> — KHÁCH thì server BỎ QUA <paramref name="conversationId"/>
/// client gửi, chỉ join hội thoại CỦA CHÍNH MÌNH (resolve cookie); NHÂN VIÊN join đúng hội thoại yêu cầu (được xem mọi
/// hội thoại resort). <see cref="JoinBoard"/> — CHỈ nhân viên (group toàn resort). Realtime chỉ tăng tốc; polling luôn là fallback.</para>
/// </summary>
[AllowAnonymous]
public sealed class ChatHub : Hub
{
    private readonly ConciergeHubAuthorizer _authorizer;
    private readonly IResortSettingsQuery _settingsQuery;

    public ChatHub(ConciergeHubAuthorizer authorizer, IResortSettingsQuery settingsQuery)
    {
        _authorizer = authorizer;
        _settingsQuery = settingsQuery;
    }

    /// <summary>Join nhóm realtime của MỘT hội thoại. Nhân viên (claim role) → join <c>conversation-{conversationId}</c>
    /// yêu cầu. Khách → BỎ QUA id gửi lên, resolve hội thoại của chính mình rồi join; cookie/phiên hỏng → <see cref="HubException"/>.</summary>
    public async Task JoinConversation(Guid conversationId)
    {
        if (ConciergeHubAuthorizer.IsStaff(Context.User))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ConciergeHubGroups.Conversation(conversationId)).ConfigureAwait(false);
            return;
        }

        var (sessionCookie, roomId) = ReadGuestConnectionInfo();
        var resolution = await _authorizer
            .ResolveGuestConversationAsync(sessionCookie, roomId, Context.ConnectionAborted)
            .ConfigureAwait(false);
        if (!resolution.ContextValid)
        {
            throw new HubException("guest_context_missing");
        }

        // Khách chưa mở hội thoại nào → không có gì để join (client join lại sau khi gửi tin đầu tiên).
        if (resolution.ConversationId is { } ownConversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ConciergeHubGroups.Conversation(ownConversationId)).ConfigureAwait(false);
        }
    }

    /// <summary>Rời nhóm hội thoại (không cần phân quyền — chỉ ngừng nhận realtime).</summary>
    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConciergeHubGroups.Conversation(conversationId));

    /// <summary>Join nhóm board lễ tân toàn resort (nhận mọi tin/cập nhật). CHỈ nhân viên (claim role) — khách → <see cref="HubException"/>.</summary>
    public async Task JoinBoard()
    {
        if (!ConciergeHubAuthorizer.IsStaff(Context.User))
        {
            throw new HubException("forbidden");
        }

        var settings = await _settingsQuery.GetAsync(Context.ConnectionAborted).ConfigureAwait(false);
        if (settings is null)
        {
            throw new HubException("configuration_unavailable");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConciergeHubGroups.ResortStaff(settings.ResortId)).ConfigureAwait(false);
    }

    /// <summary>Đọc cookie phiên khách + roomId từ query handshake WebSocket (KHÔNG tin body/id client cho phân quyền).</summary>
    private (string? SessionCookie, Guid RoomId) ReadGuestConnectionInfo()
    {
        var http = Context.GetHttpContext();
        if (http is null)
        {
            return (null, Guid.Empty);
        }

        var sessionCookie = http.Request.Cookies[GuestAccessModule.SessionCookieName];
        _ = Guid.TryParse(http.Request.Query["roomId"], out var roomId);
        return (sessionCookie, roomId);
    }
}
