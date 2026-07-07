using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Abstractions.Security;

/// <summary>Kết quả xác minh mật khẩu (hỗ trợ rehash-on-login khi nâng tham số hashing).</summary>
public enum PasswordVerificationResult
{
    Failed = 0,
    Success = 1,

    /// <summary>Đúng mật khẩu nhưng hash dùng tham số cũ → nên hash lại &amp; lưu (nâng cấp nền).</summary>
    SuccessRehashNeeded = 2,
}

/// <summary>
/// Băm/xác minh mật khẩu (Argon2id). LƯU chuỗi PHC tự mô tả tham số → verify + rehash không phá dữ liệu.
/// KHÔNG bao giờ lưu/log mật khẩu thô.
/// </summary>
public interface IPasswordHasher : ISingletonService
{
    /// <summary>Băm mật khẩu → chuỗi PHC (chứa salt + tham số).</summary>
    string Hash(string password);

    /// <summary>Xác minh mật khẩu so với chuỗi PHC đã lưu (hằng thời gian; không throw khi hash hỏng → Failed).</summary>
    PasswordVerificationResult Verify(string hashedPassword, string providedPassword);
}
