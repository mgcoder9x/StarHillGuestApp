namespace Bedrock.Application.Ports.ExternalAuth;

/// <summary>
/// Yêu cầu bắt đầu đăng nhập ngoài (F27). <see cref="ReturnUrl"/> là nơi app muốn quay về sau khi xong —
/// adapter PHẢI whitelist returnUrl (chống open-redirect). Field shape do AI chốt (AD-036) — design chỉ đặt tên.
/// </summary>
public sealed record ExternalAuthRequest(string ReturnUrl);

/// <summary>
/// Kết quả tạo challenge: <see cref="RedirectUri"/> = URL authorize của provider để app redirect user tới;
/// <see cref="State"/> = token đối chứng (app nhớ lại để khớp ở callback). PKCE code_verifier + nonce do adapter
/// GIỮ nội bộ (keyed theo State) — KHÔNG lộ ra port (giữ port tối thiểu). Shape do AI chốt (AD-036).
/// </summary>
public sealed record ExternalAuthChallenge(Uri RedirectUri, string State);

/// <summary>
/// Tham số callback từ provider: <see cref="Code"/> (authorization code) + <see cref="State"/> (khớp với
/// challenge). Adapter verify state/PKCE/nonce/replay nội bộ; lỗi → ném (không lộ chi tiết provider). Shape do
/// AI chốt (AD-036).
/// </summary>
public sealed record ExternalAuthCallback(string State, string Code);

/// <summary>
/// Hồ sơ người dùng chuẩn hoá từ provider. <see cref="Email"/>/<see cref="EmailVerified"/> NULLABLE — KHÔNG giả
/// định mọi provider trả email (vd Zalo). Policy account-linking / email-trust ở <c>Identity.Application</c>
/// (nghiệp vụ — KHÔNG ở lõi, F3).
/// </summary>
public sealed record ExternalUserProfile(
    string Provider,
    string ProviderUserId,
    string? Email,
    bool? EmailVerified,
    string? DisplayName);

/// <summary>
/// Provider đăng nhập ngoài (contract-first, F27). Multi-implementation CÓ CHỦ ĐÍCH (google/zalo/... — mỗi
/// adapter một impl); resolve qua <see cref="IExternalAuthProviderRegistry"/> theo <see cref="Name"/>.
/// </summary>
public interface IExternalAuthProvider
{
    /// <summary>Tên định danh provider (vd "google", "zalo") — khóa để registry resolve.</summary>
    string Name { get; }

    /// <summary>Tạo challenge (state + PKCE + nonce + returnUrl-whitelist do adapter dựng) để redirect user.</summary>
    Task<ExternalAuthChallenge> CreateChallengeAsync(ExternalAuthRequest request, CancellationToken ct = default);

    /// <summary>Hoàn tất: verify state/PKCE/replay rồi đổi code → hồ sơ người dùng chuẩn hoá.</summary>
    Task<ExternalUserProfile> CompleteAsync(ExternalAuthCallback callback, CancellationToken ct = default);
}

/// <summary>Resolve <see cref="IExternalAuthProvider"/> theo tên (multi-impl registry — F27).</summary>
public interface IExternalAuthProviderRegistry
{
    IExternalAuthProvider Resolve(string name);
}
