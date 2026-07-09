namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Tính exponential backoff cho retry outbox (R8.5). Tách khỏi dispatcher (generic) để thuần + test được
/// độc lập, đồng thời tránh static member trên generic type (CA1000).
/// </summary>
internal static class OutboxBackoff
{
    /// <summary>
    /// Trả độ trễ chờ trước lần thử kế: <c>BaseDelay × 2^(errorCount-1)</c>, cắp ở <c>MaxDelay</c>.
    /// <paramref name="errorCount"/> là số lần đã thất bại (≥1). Jitter (nếu có) do phía gọi cộng thêm.
    /// </summary>
    public static TimeSpan ComputeBackoff(int errorCount, OutboxDispatcherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var steps = Math.Max(0, errorCount - 1);
        var clampedSteps = Math.Min(steps, 40); // 2^40 đã vượt xa mọi MaxDelay hợp lý → tránh overflow double.
        var factor = Math.Pow(2, clampedSteps);
        var ticks = options.BaseDelay.Ticks * factor;
        var capped = Math.Min(ticks, options.MaxDelay.Ticks);
        return TimeSpan.FromTicks((long)capped);
    }
}
