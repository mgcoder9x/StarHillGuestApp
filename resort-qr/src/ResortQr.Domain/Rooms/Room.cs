using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Rooms;

/// <summary>
/// Phòng của resort. Xóa mềm (<see cref="ISoftDeletable"/>) + audit + concurrency (xmin).
/// Số phòng unique theo resort trong phạm vi chưa xóa (partial unique <c>ux_room_number</c>).
/// </summary>
public sealed class Room : AuditableEntity, ISoftDeletable
{
    public required Guid ResortId { get; set; }

    public required string RoomNumber { get; set; }

    public string? Building { get; set; }

    public int? Floor { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
