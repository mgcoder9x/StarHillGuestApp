using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bedrock.Api.Observability;

/// <summary>
/// Tên + nguồn telemetry dùng chung của base (F34/§9.3). Component (Api/Infrastructure/module) phát span/metric
/// dưới nguồn <see cref="Name"/> để <c>AddBedrockObservability</c> gom (AddSource/AddMeter) — một tên nhất quán,
/// tránh mỗi nơi tự đặt. <see cref="ActivitySource"/>/<see cref="Meter"/> sống theo vòng đời process (không dispose).
/// </summary>
public static class BedrockTelemetry
{
    /// <summary>Tên chung cho ActivitySource + Meter của base.</summary>
    public const string Name = "Bedrock";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);
}
