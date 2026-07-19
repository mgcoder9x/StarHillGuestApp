namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Tham số worker outbox (per-module — design §7.2). Mỗi module đăng ký dispatcher của mình qua
/// <c>AddOutboxDispatcher&lt;TContext&gt;()</c> và có thể tinh chỉnh riêng (named options theo context).
/// </summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>Số message tối đa claim trong một lượt poll (giữ transaction ngắn).</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Ngưỡng số lần publish thất bại → cách ly dead-letter (R8.5, không retry vô hạn).</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Độ trễ nền của exponential backoff (lần fail đầu). Thực tế = Base × 2^(errorCount-1), cắp ở <see cref="MaxDelay"/>.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Trần độ trễ giữa các lần retry (chống backoff phình vô hạn).</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Thời gian giữ claim; dispatcher gia hạn trước mỗi message nên phải dài hơn worst-case một publish.</summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Interval for refreshing backlog/oldest-pending/dead-letter gauges. The dispatcher throttles this per module
    /// so a fast poll loop does not turn operational telemetry into constant database load.
    /// </summary>
    public TimeSpan OperationalMetricsRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Stable value for the <c>module</c> metric tag; null uses the DbContext type name.</summary>
    public string? TelemetryName { get; set; }

    /// <summary>Khóa named-options theo kiểu DbContext → mỗi module có cấu hình dispatcher độc lập.</summary>
    internal static string KeyFor<TContext>() => typeof(TContext).FullName ?? typeof(TContext).Name;

    internal static bool IsValid(OutboxDispatcherOptions options) =>
        options.BatchSize > 0
        && options.MaxAttempts > 0
        && options.BaseDelay > TimeSpan.Zero
        && options.MaxDelay >= options.BaseDelay
        && options.ClaimLease > TimeSpan.Zero
        && options.OperationalMetricsRefreshInterval > TimeSpan.Zero;

    internal static string TelemetryNameFor<TContext>(OutboxDispatcherOptions options) =>
        string.IsNullOrWhiteSpace(options.TelemetryName)
            ? typeof(TContext).Name
            : options.TelemetryName.Trim();
}
