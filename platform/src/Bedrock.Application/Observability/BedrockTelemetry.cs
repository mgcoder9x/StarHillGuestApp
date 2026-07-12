using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bedrock.Application.Observability;

/// <summary>
/// Tên + nguồn telemetry dùng chung của base (F34/§9.3). Đặt ở <b>Bedrock.Application</b> (shared kernel) để MỌI
/// tầng — Api, Infrastructure, module — cùng phát span/metric dưới một nguồn <see cref="Name"/> nhất quán
/// (AddSource/AddMeter gom một tên). Trước đây ở Bedrock.Api → Infrastructure (Api⊥Infra) KHÔNG emit được metric
/// hạ tầng (outbox-lag/dead-letter) dưới nguồn chung; chuyển xuống Application gỡ đúng nút đó (khuyến nghị N-046).
/// <see cref="ActivitySource"/>/<see cref="Meter"/> sống theo vòng đời process (không dispose).
/// </summary>
public static class BedrockTelemetry
{
    /// <summary>Tên chung cho ActivitySource + Meter của base.</summary>
    public const string Name = "Bedrock";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);
}
