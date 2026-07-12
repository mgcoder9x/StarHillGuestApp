using Bedrock.Domain.Entities;

namespace Rooms.Domain;

/// <summary>
/// Phòng của resort. Xóa mềm (<see cref="ISoftDeletable"/>) + audit + concurrency token (xmin trên Npgsql).
/// Số phòng unique theo resort trong phạm vi CHƯA xóa (partial unique <c>ux_room_number</c>). Port từ resort-qr
/// (SharedKernel→Bedrock; thêm <see cref="IHasConcurrencyToken"/> vì AuditableEntity Bedrock không mang concurrency).
/// ResortId là Guid TRẦN (không FK chéo-schema sang ResortConfig — QR-DV-003).
/// </summary>
public sealed class Room : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    public required string RoomNumber { get; set; }

    public string? Building { get; set; }

    public int? Floor { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Optimistic concurrency token (map → xmin trên Npgsql qua PlatformDbContext).</summary>
    public uint RowVersion { get; set; }
}
