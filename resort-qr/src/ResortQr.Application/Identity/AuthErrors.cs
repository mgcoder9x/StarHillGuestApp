using ResortQr.SharedKernel.Results;

namespace ResortQr.Application.Identity;

/// <summary>
/// Mã lỗi auth (Unauthorized → 401). Login trả lỗi MƠ HỒ ĐỒNG NHẤT (không lộ email tồn tại)
/// để chống user-enumeration (§12 §4).
/// </summary>
public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("invalid_credentials", "Email hoặc mật khẩu không đúng.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("invalid_refresh_token", "Refresh token không hợp lệ.");

    public static Error RefreshTokenExpired =>
        Error.Unauthorized("refresh_token_expired", "Phiên đã hết hạn, vui lòng đăng nhập lại.");
}
