namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Phía WORKER (background), KHÔNG lộ cho use case (CP11): claim nguyên tử batch pending → publish qua
/// <see cref="IEventBusPublisher"/> → mark processed; fail → backoff (<c>NextAttemptAt</c>); vượt ngưỡng →
/// dead-letter (<c>DeadLetteredAt</c>) — AD-016.
/// <para>
/// <b>Hợp đồng giao (A-09/AD-086):</b> đảm bảo <b>at-least-once</b>, <b>KHÔNG cam kết ordering</b> giao (không
/// global, không per-aggregate FIFO). Việc claim <c>ORDER BY occurred_at</c> chỉ là heuristic best-effort — với
/// nhiều dispatcher đồng thời (SKIP LOCKED) + backoff per-message, event có thể giao KHÔNG theo thứ tự phát sinh.
/// Mỗi message xử lý ĐỘC LẬP: một message lỗi (backoff/dead-letter) KHÔNG chặn message khác trong batch. Consumer
/// phải tolerant thứ tự (dùng Inbox khử trùng + logic idempotent). Cần ordered delivery → mở rộng PartitionKey +
/// sequence (KHÔNG phải mặc định base — design §7.2 "Không cam kết global ordering").
/// </para>
/// </summary>
public interface IOutboxDispatcher
{
    Task DispatchPendingAsync(CancellationToken ct = default);
}
