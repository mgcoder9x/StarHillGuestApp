namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Tham số cho worker LÊN LỊCH chạy dispatcher outbox (<see cref="OutboxDispatcherHostedService{TContext}"/>).
/// <para>
/// <b>Vì sao tách khỏi <see cref="OutboxDispatcherOptions"/>:</b> Options kia mô tả HÀNH VI một lượt phát
/// (batch/backoff/dead-letter — luôn cần khi <c>DispatchPendingAsync</c> chạy). Options này mô tả LỊCH chạy
/// (bao lâu poll một lần) — chỉ liên quan khi Host bật worker opt-in. Tách để hành vi phát không phụ thuộc
/// việc ai/khi nào lên lịch (design §7.2; AD-047 giữ "Host quyết lịch").
/// </para>
/// Per-module (named options theo <c>TContext</c>) như dispatcher — mỗi module đặt nhịp poll riêng.
/// </summary>
public sealed class OutboxDispatcherWorkerOptions
{
    /// <summary>
    /// Khoảng nghỉ GIỮA hai lượt poll (sau khi một lượt <c>DispatchPendingAsync</c> hoàn tất). Mặc định 5 giây:
    /// đủ nhỏ để độ trễ phát thấp, đủ lớn để không nện DB khi outbox rỗng. Host tinh chỉnh theo SLA phát sự kiện.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Khóa named-options theo kiểu DbContext → mỗi module có nhịp poll worker độc lập.</summary>
    internal static string KeyFor<TContext>() => typeof(TContext).FullName ?? typeof(TContext).Name;
}
