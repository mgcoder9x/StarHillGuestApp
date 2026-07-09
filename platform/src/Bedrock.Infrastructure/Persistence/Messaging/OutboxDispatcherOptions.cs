namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Tham số worker outbox (per-module — design §7.2). Mỗi module đăng ký dispatcher của mình qua
/// <c>AddOutboxDispatcher&lt;TContext&gt;()</c> và có thể tinh chỉnh riêng (named options theo context).
/// </summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>Số message tối đa claim trong một lượt poll (giữ transaction ngắn).</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Ngưỡng số lần publish thất bại → cách ly dead-letter (R8.5, không retry vô hạn).</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Độ trễ nền của exponential backoff (lần fail đầu). Thực tế = Base × 2^(errorCount-1), cắp ở <see cref="MaxDelay"/>.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Trần độ trễ giữa các lần retry (chống backoff phình vô hạn).</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Khóa named-options theo kiểu DbContext → mỗi module có cấu hình dispatcher độc lập.</summary>
    internal static string KeyFor<TContext>() => typeof(TContext).FullName ?? typeof(TContext).Name;
}
