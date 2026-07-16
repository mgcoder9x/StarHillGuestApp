using Bedrock.Domain.Entities;

namespace Concierge.Domain;

/// <summary>
/// Ghi chú nội bộ của nhân viên (Req 9.4) — gắn theo phòng (<see cref="RoomId"/>) HOẶC theo hội thoại
/// (<see cref="ConversationId"/>), cả hai đều tuỳ chọn. TUYỆT ĐỐI KHÔNG lộ cho khách (chỉ endpoint admin RequireStaff).
/// Mọi Id (<see cref="ResortId"/>/<see cref="RoomId"/>/<see cref="AuthorUserId"/>) là Guid TRẦN — KHÔNG FK chéo-schema
/// (QR-AD-002); riêng <see cref="ConversationId"/> là FK NỘI-schema→<see cref="Conversation"/> (Restrict — giữ vết,
/// không xoá hội thoại còn ghi chú trỏ tới). Sửa được → mang concurrency token (xmin, CP15). <see cref="Body"/> plain text.
/// </summary>
public sealed class InternalNote : Entity, IHasConcurrencyToken
{
    public required Guid ResortId { get; set; }

    /// <summary>Phòng mà ghi chú gắn tới (null nếu chỉ gắn hội thoại).</summary>
    public Guid? RoomId { get; set; }

    /// <summary>Hội thoại mà ghi chú gắn tới (null nếu chỉ gắn phòng). FK nội-schema Restrict.</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>Nhân viên tạo ghi chú (Guid trần — không FK identity).</summary>
    public required Guid AuthorUserId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public uint RowVersion { get; set; }
}
