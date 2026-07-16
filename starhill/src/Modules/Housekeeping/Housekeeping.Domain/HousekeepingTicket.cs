using Bedrock.Domain.Entities;

namespace Housekeeping.Domain;

/// <summary>
/// Yêu cầu dọn phòng (Req 6). Tạo bởi khách (guest session/visit) hoặc nhân viên chủ động (Req 6.9). Đúng MỘT ticket
/// MỞ (<see cref="HousekeepingStatus.Requested"/>/<see cref="HousekeepingStatus.InProgress"/>) mỗi phòng — ràng buộc
/// partial unique <c>ux_hk_open_ticket_room</c> (Req 6.2). Mọi Id (<see cref="ResortId"/>/<see cref="RoomId"/>/
/// <see cref="RequestedByGuestSessionId"/>/<see cref="GuestVisitId"/>/<see cref="CompletedByUserId"/>) là Guid TRẦN —
/// KHÔNG FK chéo-schema (QR-AD-002). Concurrency token (xmin) chống hai nhân viên hoàn tất đè âm thầm (CP15).
/// </summary>
public sealed class HousekeepingTicket : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    public required Guid RoomId { get; set; }

    /// <summary>Guest session tạo yêu cầu (null nếu nhân viên chủ động tạo — Req 6.9).</summary>
    public Guid? RequestedByGuestSessionId { get; set; }

    /// <summary>Lượt lưu trú tạo yêu cầu (null nếu nhân viên tạo) — cascade huỷ khi visit kết thúc (CP9/C-GA.5).</summary>
    public Guid? GuestVisitId { get; set; }

    public HousekeepingStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Nhân viên hoàn tất (Guid trần — không FK identity). Set khi chuyển Done.</summary>
    public Guid? CompletedByUserId { get; set; }

    public HousekeepingCompletionMethod? CompletionMethod { get; set; }

    public uint RowVersion { get; set; }
}
