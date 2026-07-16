using Bedrock.Domain.Results;

namespace Identity.Domain;

/// <summary>
/// Catalog mã lỗi nghiệp vụ của module Identity (design §4.4 — mã lỗi riêng khai ở module, KHÔNG ở Bedrock).
/// Code ỔN ĐỊNH (hợp đồng máy-đọc với client, F20) + message English trung lập; localization ở tầng UI theo code.
/// </summary>
public static class AuthErrors
{
    /// <summary>
    /// Refresh token không hợp lệ / hết hạn / đã bị thu hồi. CỐ Ý dùng MỘT mã chung cho mọi trường hợp fail
    /// (không tồn tại, hết hạn, đã revoke, thua race) — KHÔNG tiết lộ lý do cụ thể cho client (chống dò token/oracle).
    /// </summary>
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("identity.invalid_refresh_token", "The refresh token is invalid, expired, or has been revoked.");

    /// <summary>
    /// Đăng nhập thất bại. CỐ Ý dùng MỘT mã chung cho MỌI nguyên nhân (username không tồn tại / sai mật khẩu /
    /// tài khoản bị vô hiệu hoá) — KHÔNG tiết lộ nguyên nhân cụ thể (chống user-enumeration/oracle, F.1b/QR-AD-039).
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "The username or password is incorrect.");
}
