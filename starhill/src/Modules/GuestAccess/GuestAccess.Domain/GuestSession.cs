using Bedrock.Domain.Entities;

namespace GuestAccess.Domain;

/// <summary>
/// Định danh THIẾT BỊ khách (cookie dài hạn). DB CHỈ lưu HASH của cookie (raw chỉ nằm ở cookie/memory — QR-AD-025).
/// Session là toàn deployment, KHÔNG gắn ResortId (ResortId/RoomId nằm ở <see cref="GuestVisit"/> — QR-TO-005).
/// Không lưu user-agent/IP/fingerprint ở giai đoạn này (tránh dữ liệu nhận dạng không cần thiết).
/// </summary>
public sealed class GuestSession : Entity
{
    /// <summary>SHA-256 (64 hex ký tự) của raw session key. Unique <c>ux_guest_session_key_hash</c>.</summary>
    public required string SessionKeyHash { get; set; }

    /// <summary>Ngôn ngữ ưu tiên của thiết bị (nếu biết) — dùng cho resolve/i18n sau.</summary>
    public string? PreferredLanguage { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}
