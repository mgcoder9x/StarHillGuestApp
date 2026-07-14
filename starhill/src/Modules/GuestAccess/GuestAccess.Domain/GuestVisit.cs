using Bedrock.Domain.Entities;

namespace GuestAccess.Domain;

/// <summary>
/// Lượt lưu trú của một phòng gắn với một thiết bị (<see cref="GuestSession"/>). Đúng MỘT visit Active mỗi
/// (session, room) — partial unique <c>ux_guest_visit_active</c>. <see cref="ResortId"/>/<see cref="RoomId"/> là
/// Guid TRẦN (không FK chéo-schema sang rooms/resort_config — QR-AD-024). Kết thúc khi lễ tân đóng hoặc idle quá hạn.
/// </summary>
public sealed class GuestVisit : Entity
{
    public required Guid ResortId { get; set; }

    public required Guid RoomId { get; set; }

    public required Guid GuestSessionId { get; set; }

    public GuestVisitStatus Status { get; set; } = GuestVisitStatus.Active;

    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Hoạt động guest gần nhất ĐƯỢC CHẤP NHẬN (portal-window/idle đều suy từ mốc này).</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Vật hoá = <see cref="LastSeenAt"/> + VisitIdleExpiryHours; dùng cho lazy expiry + background sweeper.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public Guid? ClosedByUserId { get; set; }
}
