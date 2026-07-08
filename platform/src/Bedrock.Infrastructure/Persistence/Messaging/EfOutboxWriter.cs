using System.Diagnostics;
using System.Text.Json;
using Bedrock.Application.Messaging;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Messaging.Contracts;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Impl <see cref="IOutboxWriter"/>: GHI <see cref="OutboxMessage"/> vào ChangeTracker của
/// <see cref="PlatformDbContext"/> — KHÔNG tự commit. Phải gọi bên trong
/// <c>IUnitOfWork.ExecuteInTransactionAsync</c> để outbox + thay đổi state vào CÙNG một commit (CP6/F5).
/// <para>
/// Serialize payload theo KIỂU THỰC của event (không phải base <see cref="IntegrationEvent"/>) để không mất
/// property của record dẫn xuất; options cố định (AD-015). <c>OutboxMessage.Id = IntegrationEvent.Id</c> để id
/// event chảy xuyên suốt outbox → bus → inbox (idempotency phía consumer khoá trên id này — AD-029).
/// <c>CorrelationId</c> lấy từ <see cref="Activity.Current"/> (W3C traceparent — F34/F21).
/// </para>
/// </summary>
public sealed class EfOutboxWriter(PlatformDbContext context) : IOutboxWriter
{
    public Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(
            integrationEvent, integrationEvent.GetType(), OutboxSerialization.Options);

        var message = new OutboxMessage
        {
            Id = integrationEvent.Id,
            EventType = integrationEvent.EventType,
            SchemaVersion = integrationEvent.SchemaVersion,
            Payload = payload,
            OccurredAt = integrationEvent.OccurredAt,
            CorrelationId = Activity.Current?.Id,
        };

        context.Set<OutboxMessage>().Add(message);
        return Task.CompletedTask;
    }
}
