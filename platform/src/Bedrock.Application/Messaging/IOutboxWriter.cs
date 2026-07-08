using Bedrock.Messaging.Contracts;

namespace Bedrock.Application.Messaging;

/// <summary>
/// Port DUY NHẤT mà use case được thấy để phát integration event (F25/I8). Tên "Enqueue" (không "Publish")
/// để lập trình viên KHÔNG publish thẳng bus: chỉ GHI vào outbox trong CÙNG transaction với thay đổi state.
/// Publish thực tế do worker (<c>Messaging.Dispatch</c>) đảm nhận — namespace tách riêng để CP11 kiểm bằng máy.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Ghi event vào outbox (chưa publish). Gọi bên trong <c>IUnitOfWork.ExecuteInTransactionAsync</c>.</summary>
    Task EnqueueAsync(IntegrationEvent integrationEvent, CancellationToken ct = default);
}
