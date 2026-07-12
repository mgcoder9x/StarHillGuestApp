using Bedrock.Domain.Entities;

namespace Rooms.Domain;

/// <summary>
/// Token QR nhận diện phòng (capability token plaintext, unique toàn cục). Chỉ Active/Revoked, rotate thủ công —
/// KHÔNG auto-expire (CP2). Đúng MỘT token Active mỗi phòng (partial unique <c>ux_qr_active</c>). Lịch sử revoke
/// giữ lại (không xóa) để truy vết (Req 7.4). RoomId là Guid trần tới Room (cùng schema rooms — có FK nội-module).
/// </summary>
public sealed class RoomQrToken : Entity
{
    public required Guid RoomId { get; set; }

    /// <summary>Chuỗi capability token plaintext (unique toàn cục — ux_qrtoken_token).</summary>
    public required string Token { get; set; }

    /// <summary>Dạng che để hiển thị/đối soát mà không lộ token đầy đủ (Req 11.6).</summary>
    public required string TokenPreview { get; set; }

    public RoomQrTokenStatus Status { get; set; } = RoomQrTokenStatus.Active;

    /// <summary>Số lần rotate của phòng (tăng dần).</summary>
    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; set; }

    public string? RevocationReason { get; set; }
}
