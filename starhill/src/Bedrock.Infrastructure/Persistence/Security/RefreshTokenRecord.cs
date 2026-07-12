namespace Bedrock.Infrastructure.Persistence.Security;

/// <summary>
/// Shape lưu trữ refresh token (persistence-facing) — <b>ẩn trong Infrastructure</b> (F19/AD-010): Application
/// chỉ thấy port <c>IRefreshTokenStore</c> + <c>RefreshTokenSnapshot</c>, KHÔNG thấy type này. <c>internal</c>
/// để khoá encapsulation ở mức compiler. Chỉ lưu HASH (SHA-256 hex), KHÔNG secret thô. KHÔNG audit/xmin
/// (refresh token chỉ tạo/thu hồi, không sửa nội dung → không cần concurrency token — persistence §4/§8).
/// </summary>
internal sealed class RefreshTokenRecord
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>Nhóm chuỗi rotation — reuse-detection thu hồi cả family (F10).</summary>
    public required Guid FamilyId { get; init; }

    public required string TokenHash { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Khác null = đã thu hồi (consume/rotate/reuse). Mutable vì ExecuteUpdate set nguyên tử.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? RevokedReason { get; set; }
}
