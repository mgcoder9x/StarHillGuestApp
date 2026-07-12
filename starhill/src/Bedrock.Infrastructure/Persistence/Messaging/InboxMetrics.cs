using System.Diagnostics.Metrics;
using Bedrock.Application.Observability;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Metric quan sát CONSUME (R24.3/§7.2) phát dưới Meter chung <c>Bedrock</c> — đối xứng <see cref="OutboxMetrics"/>
/// phía publish. Đo trong <c>EfIntegrationEventDispatcher.DispatchAsync</c> (core agnostic — mọi transport đi qua):
/// <list type="bullet">
///   <item><c>bedrock.inbox.dispatched</c> — counter (tag <c>outcome</c>): số message consume đã dispatch, phân theo
///   kết quả <c>handled</c>/<c>duplicate</c>/<c>dead_lettered</c>/<c>failed</c> (failed = handler NÉM → NACK/redeliver).</item>
///   <item><c>bedrock.inbox.processing.duration</c> — histogram (s): thời gian xử lý một message (R24.3 "consumer time").</item>
/// </list>
/// Instrument tĩnh (Meter sống theo process). Tag <c>outcome</c> cho phép alert theo tỉ lệ failed/dead_lettered.
/// </summary>
internal static class InboxMetrics
{
    private static readonly Counter<long> DispatchedCounter = BedrockTelemetry.Meter.CreateCounter<long>(
        "bedrock.inbox.dispatched", unit: "{message}",
        description: "Số message consume đã dispatch, tag outcome=handled|duplicate|dead_lettered|failed.");

    private static readonly Histogram<double> ProcessingDuration = BedrockTelemetry.Meter.CreateHistogram<double>(
        "bedrock.inbox.processing.duration", unit: "s",
        description: "Thời gian xử lý một message consume (dispatch → outcome) — R24.3 consumer time.");

    // Tên outcome ổn định (dùng cho tag + test). "failed" KHÔNG phải InboxDispatchOutcome (lỗi handler = ném exception).
    public const string OutcomeHandled = "handled";
    public const string OutcomeDuplicate = "duplicate";
    public const string OutcomeDeadLettered = "dead_lettered";
    public const string OutcomeFailed = "failed";

    public static void Record(string outcome, TimeSpan duration)
    {
        var tag = new KeyValuePair<string, object?>("outcome", outcome);
        DispatchedCounter.Add(1, tag);
        ProcessingDuration.Record(duration.TotalSeconds, tag);
    }
}
