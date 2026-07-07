using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.Rooms;

/// <summary>
/// Token QR nhận diện phòng (capability token plaintext, unique toàn cục). Chỉ Active/Revoked,
/// rotate thủ công — KHÔNG auto-expire. Đúng một token Active mỗi phòng (partial unique <c>ux_qr_active</c>).
/// Lịch sử revoke giữ lại (không xóa) để truy vết.
/// </summary>
public sealed class RoomQrToken : Entity
{
    public required Guid RoomId { get; set; }

    /// <summary>Chuỗi capability token plaintext (unique toàn cục).</summary>
    public required string Token { get; set; }

    /// <summary>Dạng che để hiển thị (vd 4 ký tự cuối).</summary>
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
