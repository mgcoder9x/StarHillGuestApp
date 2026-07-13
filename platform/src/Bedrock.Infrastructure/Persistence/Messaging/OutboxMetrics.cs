using System.Diagnostics.Metrics;
using Bedrock.Application.Observability;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Metric quan sát Outbox (R24.3/§7.2) phát dưới Meter chung <c>Bedrock</c> (<see cref="BedrockTelemetry.Meter"/>):
/// <list type="bullet">
///   <item><c>bedrock.outbox.published</c> — counter: số event publish thành công tới bus.</item>
///   <item><c>bedrock.outbox.dead_lettered</c> — counter: số event bị dead-letter (vượt MaxAttempts). = "dead-letter count".</item>
///   <item><c>bedrock.outbox.lease_lost</c> — counter: dispatcher mất ownership trước khi finalize.</item>
///   <item><c>bedrock.outbox.publish.lag</c> — histogram (s): tuổi message lúc publish (<c>now - occurred_at</c>) → proxy "outbox lag".</item>
/// </list>
/// <para>
/// Đo INLINE trong dispatcher (không DB-poll): tránh gauge callback sync-over-async + scoped-DbContext-trong-gauge.
/// "Publish lag" (tuổi-lúc-publish) là tín hiệu trễ end-to-end thực dụng; "oldest pending age" tuyệt đối cần query
/// pending riêng → hoãn (tradeoff TO-011). Instrument tĩnh (Meter sống theo process).
/// </para>
/// </summary>
internal static class OutboxMetrics
{
    private static readonly Counter<long> PublishedCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.published", unit: "{message}",
        description: "Số integration event outbox publish thành công tới bus.");

    private static readonly Counter<long> DeadLetteredCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.dead_lettered", unit: "{message}",
        description: "Số integration event outbox bị dead-letter (vượt MaxAttempts) — R24.3.");

    private static readonly Counter<long> LeaseLostCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.lease_lost", unit: "{message}",
        description: "Số integration event outbox mất lease ownership trước khi finalize.");

    private static readonly Histogram<double> PublishLagHistogram = BedrockTelemetry.Meter.CreateHistogram<double>(
        "bedrock.outbox.publish.lag", unit: "s",
        description: "Độ trễ outbox: giây từ occurred_at tới lúc publish thành công (proxy outbox lag) — R24.3.");

    /// <summary>Ghi nhận một event publish thành công + độ trễ (giây, kẹp >= 0 chống lệch clock).</summary>
    public static void RecordPublished(TimeSpan lag)
    {
        PublishedCounter.Add(1);
        PublishLagHistogram.Record(Math.Max(0, lag.TotalSeconds));
    }

    /// <summary>Ghi nhận một event bị dead-letter (R24.3 dead-letter count).</summary>
    public static void RecordDeadLettered() => DeadLetteredCounter.Add(1);

    public static void RecordLeaseLost() => LeaseLostCounter.Add(1);
}
