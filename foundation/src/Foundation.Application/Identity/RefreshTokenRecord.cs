namespace Foundation.Application.Identity;

/// <summary>
/// Bản ghi refresh token (persistence-facing). Chỉ lưu <see cref="TokenHash"/> (không lưu secret thô).
/// <see cref="FamilyId"/> nhóm chuỗi rotation để reuse-detection thu hồi cả family.
/// </summary>
public sealed class RefreshTokenRecord
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required Guid FamilyId { get; init; }

    public required string TokenHash { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? RevokedReason { get; set; }

    public bool IsActiveAt(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
