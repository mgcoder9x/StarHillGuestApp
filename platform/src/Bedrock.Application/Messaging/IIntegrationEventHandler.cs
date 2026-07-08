using System.Diagnostics.CodeAnalysis;
using Bedrock.Messaging.Contracts;

namespace Bedrock.Application.Messaging;

/// <summary>
/// Handler cho một loại <typeparamref name="TEvent"/> (multi-implementation — đăng ký <c>IEnumerable</c> có
/// chủ đích, F18). Chạy sau khi <c>IInboxStore</c> xác nhận là lần xử lý đầu tiên (idempotent).
/// Đuôi <c>EventHandler</c> là chủ đích (ngữ nghĩa seam messaging) — suppress <c>CA1711</c> có lý do (N-005).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Đuôi 'EventHandler' phản ánh đúng vai trò seam integration-event; đổi tên làm mờ ngữ nghĩa (N-005/F25).")]
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
