using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Bedrock.Application.Observability;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Process-local outbox metrics. Counters and histograms are emitted inline; backlog gauges are refreshed by the
/// dispatcher into an in-memory snapshot so observable callbacks never perform async database work.
/// Every measurement carries a stable module tag, allowing multiple module DbContexts to share one Meter safely.
/// </summary>
internal static class OutboxMetrics
{
    private static readonly ConcurrentDictionary<string, Snapshot> Snapshots = new(StringComparer.Ordinal);

    private static readonly Counter<long> PublishedCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.published", unit: "{message}",
        description: "Successful integration-event publishes by module.");

    private static readonly Counter<long> DeadLetteredCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.dead_lettered", unit: "{message}",
        description: "Integration events quarantined after the retry limit by module.");

    private static readonly Counter<long> LeaseLostCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.outbox.lease_lost", unit: "{message}",
        description: "Outbox messages whose claim ownership was lost before finalize.");

    private static readonly Histogram<double> PublishLagHistogram = BedrockTelemetry.Meter.CreateHistogram<double>(
        "bedrock.outbox.publish.lag", unit: "s",
        description: "Seconds from event occurrence to successful outbox publish.");

    private static readonly ObservableGauge<long> PendingGauge = BedrockTelemetry.Meter.CreateObservableGauge(
        "bedrock.outbox.pending",
        ObservePending,
        unit: "{message}",
        description: "Pending outbox messages by module.");

    private static readonly ObservableGauge<double> OldestPendingAgeGauge = BedrockTelemetry.Meter.CreateObservableGauge(
        "bedrock.outbox.oldest_pending.age",
        ObserveOldestPendingAge,
        unit: "s",
        description: "Age of the oldest pending outbox message by module.");

    private static readonly ObservableGauge<long> DeadLetterDepthGauge = BedrockTelemetry.Meter.CreateObservableGauge(
        "bedrock.outbox.dead_letter.depth",
        ObserveDeadLetterDepth,
        unit: "{message}",
        description: "Dead-lettered outbox messages retained for operator action by module.");

    public static void RecordPublished(string module, TimeSpan lag)
    {
        var tag = ModuleTag(module);
        PublishedCounter.Add(1, tag);
        PublishLagHistogram.Record(Math.Max(0, lag.TotalSeconds), tag);
    }

    public static void RecordDeadLettered(string module) => DeadLetteredCounter.Add(1, ModuleTag(module));

    public static void RecordLeaseLost(string module) => LeaseLostCounter.Add(1, ModuleTag(module));

    public static void RecordSnapshot(
        string module,
        long pendingCount,
        TimeSpan oldestPendingAge,
        long deadLetterDepth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        Snapshots[module] = new Snapshot(
            Math.Max(0, pendingCount),
            Math.Max(0, oldestPendingAge.TotalSeconds),
            Math.Max(0, deadLetterDepth));
    }

    private static Measurement<long>[] ObservePending() =>
        [.. Snapshots.Select(pair => new Measurement<long>(pair.Value.PendingCount, ModuleTag(pair.Key)))];

    private static Measurement<double>[] ObserveOldestPendingAge() =>
        [.. Snapshots.Select(pair => new Measurement<double>(pair.Value.OldestPendingAgeSeconds, ModuleTag(pair.Key)))];

    private static Measurement<long>[] ObserveDeadLetterDepth() =>
        [.. Snapshots.Select(pair => new Measurement<long>(pair.Value.DeadLetterDepth, ModuleTag(pair.Key)))];

    private static KeyValuePair<string, object?> ModuleTag(string module) => new("module", module);

    private sealed record Snapshot(long PendingCount, double OldestPendingAgeSeconds, long DeadLetterDepth);
}
