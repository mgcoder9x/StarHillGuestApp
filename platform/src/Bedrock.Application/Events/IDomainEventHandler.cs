using System.Diagnostics.CodeAnalysis;
using Bedrock.Domain.Events;

namespace Bedrock.Application.Events;

/// <summary>
/// Handler cho một loại domain event in-process <typeparamref name="TEvent"/> (R33). Chạy TRƯỚC commit,
/// trong cùng transaction với thay đổi state. "Domain event → integration event": handler inject
/// <c>IOutboxWriter</c> và enqueue trong cùng transaction (R33.3).
/// Đuôi <c>EventHandler</c> chủ đích — suppress <c>CA1711</c> (N-005).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Đuôi 'EventHandler' phản ánh đúng vai trò handler domain-event; đổi tên làm mờ ngữ nghĩa (N-005/F13).")]
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
