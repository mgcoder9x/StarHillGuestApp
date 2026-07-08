namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Phía WORKER (background), KHÔNG lộ cho use case (CP11): claim nguyên tử batch pending → publish qua
/// <see cref="IEventBusPublisher"/> → mark processed; fail → backoff (<c>NextAttemptAt</c>); vượt ngưỡng →
/// dead-letter (<c>DeadLetteredAt</c>) — AD-016.
/// </summary>
public interface IOutboxDispatcher
{
    Task DispatchPendingAsync(CancellationToken ct = default);
}
