namespace Concierge.Api.Realtime;

/// <summary>
/// Nguồn DUY NHẤT tên SignalR group của module Concierge (K-Con.4) — dùng CHUNG bởi <see cref="ChatHub"/> (join) và
/// <see cref="SignalRConciergeNotifier"/> (push) để KHÔNG drift tên group (join một tên, push tên khác = mất realtime im lặng).
/// <list type="bullet">
/// <item><c>conversation-{conversationId}</c>: khách của hội thoại (join hội thoại của mình) + nhân viên đang xem hội thoại đó.</item>
/// <item><c>resort-{resortId}-staff</c>: nhóm lễ tân online của resort (board — nhận mọi tin/cập nhật toàn resort).</item>
/// </list>
/// </summary>
public static class ConciergeHubGroups
{
    public static string Conversation(Guid conversationId) => $"conversation-{conversationId}";

    public static string ResortStaff(Guid resortId) => $"resort-{resortId}-staff";
}
