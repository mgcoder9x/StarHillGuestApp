namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Tham số dọn (retention) bảng <c>outbox_message</c> (task 7.5, R8.5). Vì dead-letter là CỘT trên chính
/// bảng (AD-003, không bảng DLQ riêng), job này giữ bảng gọn bằng cách XOÁ row đã publish (processed) quá TTL,
/// đồng thời GIỮ row dead-letter để soi/điều tra (mặc định giữ vô thời hạn). Per-module (named options theo
/// DbContext) như dispatcher — mỗi module tinh chỉnh riêng.
/// </summary>
public sealed class OutboxRetentionOptions
{
    /// <summary>Giữ row đã publish (<c>processed_at</c>) trong bao lâu trước khi xoá. Mặc định 7 ngày (đủ soi/đối soát).</summary>
    public TimeSpan ProcessedRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// TTL cho row dead-letter. <c>null</c> = GIỮ VÔ THỜI HẠN (mặc định — dead-letter cần soi/replay thủ công,
    /// không tự xoá). Đặt giá trị nếu muốn dọn dead-letter cũ sau khi đã export/xử lý.
    /// </summary>
    public TimeSpan? DeadLetterRetention { get; set; }

    /// <summary>Khóa named-options theo kiểu DbContext → mỗi module có cấu hình retention độc lập.</summary>
    internal static string KeyFor<TContext>() => typeof(TContext).FullName ?? typeof(TContext).Name;
}
