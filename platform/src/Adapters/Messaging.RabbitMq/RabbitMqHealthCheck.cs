using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Adapters.Messaging.RabbitMq;

/// <summary>
/// Readiness health-check cho RabbitMQ (design §9.6/R34, F29): khi messaging bật, broker là DEPENDENCY của cả
/// publish (worker) lẫn consume (subscriber) → <c>/health/ready</c> PHẢI phản ánh broker sống/chết. Mở một
/// connection NGẮN tới broker → <see cref="HealthStatus.Healthy"/> nếu kết nối được, <see cref="HealthStatus.Unhealthy"/>
/// nếu không (→ readiness 503 → orchestrator ngừng route traffic + pod không "ready" khi broker chết, thay vì
/// nhận traffic rồi dồn ứ outbox / consumer chết âm thầm). Tag <c>ready</c> (khớp <c>MapBedrockHealth</c>).
/// <para>
/// Connection ngắn-hạn mỗi lần probe (đơn giản + độc lập trạng thái publisher/consumer). Tradeoff TO-010:
/// nhẹ hơn nếu tái dùng connection nhưng ghép chặt — chấp nhận cho base, app tinh chỉnh tần suất probe.
/// </para>
/// </summary>
public sealed class RabbitMqHealthCheck(RabbitMqOptions options) : IHealthCheck
{
    // public (như RabbitMqEventBusPublisher) để ActivatorUtilities của AddTypeActivatedCheck tìm được ctor lúc runtime.
    private readonly ConnectionFactory _connectionFactory = RabbitMqConnectionFactory.Create(options);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection =
                await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is not open.");
        }
#pragma warning disable CA1031 // CỐ Ý bắt rộng: mọi lỗi kết nối/timeout/auth → Unhealthy (không ném ra health pipeline).
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("RabbitMQ unreachable.", ex);
        }
    }
}
