using Concierge.Application;
using Microsoft.AspNetCore.SignalR;

namespace Concierge.Api.Realtime;

/// <summary>
/// Adapter realtime THẬT (K-Con.4): hiện thực port Application <see cref="IConciergeRealtimeNotifier"/> bằng
/// <see cref="IHubContext{THub}"/> của <see cref="ChatHub"/>. Host đăng ký cái này OVERRIDE <c>NoOpConciergeRealtimeNotifier</c>
/// (mặc định ở Infrastructure) — Application vẫn THUẦN (chỉ biết port, không biết SignalR). Use case gọi notifier SAU
/// khi persist thành công (realtime chỉ tăng tốc; polling GET là nguồn sự thật/fallback). Đẩy tới:
/// <list type="bullet">
/// <item><c>conversation-{id}</c>: khách của hội thoại (thấy staff reply/close/đã-đọc).</item>
/// <item><c>resort-{resortId}-staff</c>: board lễ tân (thấy tin mới/cập nhật toàn resort).</item>
/// </list>
/// Payload = Id trần (client tự gọi GET để lấy nội dung — realtime chỉ là tín hiệu, giữ hub nhẹ + không lộ nội dung qua nhiều đường).
/// </summary>
public sealed class SignalRConciergeNotifier : IConciergeRealtimeNotifier
{
    // Tên method Server→Client (hợp đồng với client SPA) — đặt hằng để không gõ lệch chuỗi.
    private const string MessageReceived = "MessageReceived";
    private const string ConversationUpdated = "ConversationUpdated";
    private const string MessageRead = "MessageRead";

    private readonly IHubContext<ChatHub> _hub;

    public SignalRConciergeNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    /// <summary>Khách gửi tin mới → báo nhóm hội thoại (nếu nhân viên đang xem) + nhóm board lễ tân (badge/list).</summary>
    public Task NotifyMessageReceivedAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct = default)
    {
        var payload = new { conversationId, messageId };
        return Task.WhenAll(
            _hub.Clients.Group(ConciergeHubGroups.Conversation(conversationId)).SendAsync(MessageReceived, payload, ct),
            _hub.Clients.Group(ConciergeHubGroups.ResortStaff(resortId)).SendAsync(MessageReceived, payload, ct));
    }

    /// <summary>Nhân viên reply/đóng → báo nhóm hội thoại (khách) + board lễ tân (đồng bộ trạng thái).</summary>
    public Task NotifyConversationUpdatedAsync(Guid resortId, Guid conversationId, CancellationToken ct = default)
    {
        var payload = new { conversationId };
        return Task.WhenAll(
            _hub.Clients.Group(ConciergeHubGroups.Conversation(conversationId)).SendAsync(ConversationUpdated, payload, ct),
            _hub.Clients.Group(ConciergeHubGroups.ResortStaff(resortId)).SendAsync(ConversationUpdated, payload, ct));
    }

    /// <summary>Tin được đánh dấu đã đọc → báo nhóm hội thoại (khách thấy "đã xem"). KHÔNG cần đẩy board.</summary>
    public Task NotifyMessageReadAsync(Guid resortId, Guid conversationId, CancellationToken ct = default) =>
        _hub.Clients.Group(ConciergeHubGroups.Conversation(conversationId)).SendAsync(MessageRead, new { conversationId }, ct);
}
