using ResortQr.SharedKernel.Entities;

namespace ResortQr.Domain.GuestAccess;

/// <summary>
/// Thiết bị khách (cookie dài hạn). Lưu HASH của cookie (không raw). KHÔNG gắn ResortId
/// (thiết bị toàn cục theo deployment — <c>24</c> §6).
/// </summary>
public sealed class GuestSession : Entity
{
    /// <summary>Hash của cookie thiết bị (unique).</summary>
    public required string SessionKeyHash { get; set; }

    public string? PreferredLanguage { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }
}
