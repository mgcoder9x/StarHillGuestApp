using Bedrock.Domain.Entities;

namespace Housekeeping.Domain;

/// <summary>
/// Nhật ký MỘT lần chuyển trạng thái ticket (Req 6.8 — đối soát "ai làm gì, khi nào"). Append-only (bất biến, KHÔNG
/// concurrency token). Ghi trong CÙNG transaction với đổi <see cref="HousekeepingTicket.Status"/> (không mất vết).
/// FK nội-module <see cref="HousekeepingTicketId"/>→ticket (Cascade). <see cref="ActorUserId"/> Guid trần (không FK identity).
/// </summary>
public sealed class HousekeepingEvent : Entity
{
    public required Guid HousekeepingTicketId { get; set; }

    public HousekeepingStatus NewStatus { get; set; }

    public HousekeepingActorType ActorType { get; set; }

    /// <summary>Nhân viên gây chuyển trạng thái (null nếu Guest/System).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Phương thức hoàn tất (App/StaffScan) — chỉ có nghĩa khi <see cref="NewStatus"/> = Done.</summary>
    public HousekeepingCompletionMethod? Method { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
