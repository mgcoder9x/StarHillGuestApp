using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.GuestAccess;

/// <summary>
/// Lượt lưu trú/phòng của một thiết bị. Kết thúc khi lễ tân đóng hoặc idle quá hạn.
/// Đúng một visit Active mỗi (session, room) — partial unique <c>ux_visit_active</c>.
/// </summary>
public sealed class GuestVisit : Entity
{
    public required Guid ResortId { get; set; }

    public required Guid RoomId { get; set; }

    public required Guid GuestSessionId { get; set; }

    public GuestVisitStatus Status { get; set; } = GuestVisitStatus.Active;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Vật hoá = LastSeenAt + idle expiry; dùng cho background sweeper.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public Guid? ClosedByUserId { get; set; }
}
