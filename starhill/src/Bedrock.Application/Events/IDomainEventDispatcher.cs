using Bedrock.Domain.Events;

namespace Bedrock.Application.Events;

/// <summary>
/// Dispatch domain event in-process. Gọi bởi <c>PlatformDbContext.SaveChangesAsync</c> TRƯỚC commit
/// (cùng transaction — R33.1/R33.2). Vòng lặp: dispatch → handler có thể stage thêm thay đổi/phát thêm
/// event → lặp; max-depth chặn vòng vô hạn (R33.4). Không có handler → no-op (domain event là tùy chọn).
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
}
