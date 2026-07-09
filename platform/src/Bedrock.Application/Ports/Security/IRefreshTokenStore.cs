namespace Bedrock.Application.Ports.Security;

/// <summary>
/// Snapshot bất biến của refresh token phục vụ use case (F19) — DTO thuần, KHÔNG phải EF entity.
/// Shape lưu trữ thật (<c>RefreshTokenRecord</c>) ẩn trong Infrastructure; Application chỉ thấy DTO này.
/// </summary>
public sealed record RefreshTokenSnapshot(
    Guid Id,
    Guid UserId,
    Guid FamilyId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);

/// <summary>
/// Cơ chế lưu trữ refresh token nguyên tử, chống race (F5/F10/F19). Application nói qua port nghiệp vụ,
/// KHÔNG lộ shape lưu trữ. Rotation + reuse-detection dùng các thao tác dưới đây trong một transaction.
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Lấy token theo hash (SHA-256), trả về BẤT KỂ đã revoked hay hết hạn — snapshot mang <c>RevokedAt</c>/
    /// <c>ExpiresAt</c> để use case tự quyết (AD-031). Null CHỈ khi không tồn tại hash. Lý do KHÔNG lọc
    /// "active-only": reuse-detection (§7.4) phải THẤY được token đã revoked bị dùng lại để thu hồi cả family;
    /// nếu lọc active-only thì token đánh cắp (đã revoked) trả null → mất tính năng bảo mật.
    /// </summary>
    Task<RefreshTokenSnapshot?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Consume nguyên tử: <c>UPDATE ... SET revoked_at=@now, reason=@reason, replaced_by=@replacedByTokenId
    /// WHERE id=@tokenId AND revoked_at IS NULL</c>. Trả true nếu đúng 1 row bị đổi (thắng race), false nếu 0 row.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid tokenId, DateTimeOffset now, string reason, Guid replacedByTokenId, CancellationToken ct = default);

    /// <summary>Stage token mới (commit ở SaveChanges — cùng transaction với consume).</summary>
    Task AddAsync(RefreshTokenSnapshot newToken, CancellationToken ct = default);

    /// <summary>Thu hồi toàn bộ family (reuse-detection: token đã revoked bị dùng lại → revoke cả họ).</summary>
    Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default);
}
