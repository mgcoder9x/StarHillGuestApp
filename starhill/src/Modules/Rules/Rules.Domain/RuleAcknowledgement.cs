using Bedrock.Domain.Entities;

namespace Rules.Domain;

/// <summary>
/// Bản ghi khách đã xác nhận đã đọc nội quy — gắn với <see cref="GuestVisitId"/> (Req 3.7). Bản ghi VẬN HÀNH
/// (Req 11.7 — KHÔNG chứng cứ pháp lý). Unique <c>(GuestVisitId, RulePublicationId)</c> → ack idempotent trong
/// một visit. <see cref="RoomId"/>/<see cref="GuestSessionId"/>/<see cref="GuestVisitId"/> là Guid TRẦN (không FK
/// chéo-schema rooms/guest_access — QR-AD-002/024). <see cref="RulePublicationId"/> nội-schema rules (có FK Restrict).
/// </summary>
public sealed class RuleAcknowledgement : Entity
{
    public required Guid ResortId { get; set; }

    public required Guid RoomId { get; set; }

    public required Guid GuestSessionId { get; set; }

    public required Guid GuestVisitId { get; set; }

    public required Guid RulePublicationId { get; set; }

    public int Version { get; set; }

    public required string LanguageCode { get; set; }

    public DateTimeOffset AcceptedAt { get; set; }
}
