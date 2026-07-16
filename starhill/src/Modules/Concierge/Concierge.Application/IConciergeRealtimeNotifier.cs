namespace Concierge.Application;

/// <summary>
/// PORT realtime (Application-thuần) — use case gọi SAU khi persist thành công để đẩy sự kiện tới client online.
/// Impl mặc định là NO-OP (đăng ký ở Infrastructure — K-Con.2) nên Application chạy/test được KHÔNG cần SignalR;
/// Host OVERRIDE bằng <c>SignalRConciergeNotifier</c> (K-Con.4, adapter Api gói <c>IHubContext&lt;ChatHub&gt;</c>).
/// Realtime CHỈ tăng tốc — polling (<c>GET /conversation</c>) LUÔN là nguồn sự thật/fallback. Nhận Id TRẦN (không lộ entity).
/// </summary>
public interface IConciergeRealtimeNotifier
{
    /// <summary>Khách gửi tin mới → báo nhóm lễ tân của resort (badge/board cập nhật + hội thoại nếu đang mở).</summary>
    Task NotifyMessageReceivedAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct = default);

    /// <summary>Trạng thái/nội dung hội thoại đổi (nhân viên reply/đóng) → báo nhóm hội thoại.</summary>
    Task NotifyConversationUpdatedAsync(Guid resortId, Guid conversationId, CancellationToken ct = default);

    /// <summary>Tin được đánh dấu đã đọc → báo nhóm hội thoại (cập nhật "đã xem").</summary>
    Task NotifyMessageReadAsync(Guid resortId, Guid conversationId, CancellationToken ct = default);
}
