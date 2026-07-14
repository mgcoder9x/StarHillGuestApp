using System.Security.Cryptography;
using System.Text;
using GuestAccess.Application;

namespace GuestAccess.Infrastructure.Security;

/// <summary>
/// Băm khóa phiên khách bằng SHA-256 (hex thường, 64 ký tự — khớp cột <c>session_key_hash</c> maxLength 64). Raw
/// key entropy cao (CSPRNG 256-bit) nên SHA-256 là đủ + tất định để lookup. DB lưu HASH, cookie giữ raw (QR-AD-025).
/// Đăng ký singleton thủ công ở <c>AddGuestAccessInfrastructure</c> (Application port không mang marker DI).
/// </summary>
public sealed class Sha256GuestSessionKeyHasher : IGuestSessionKeyHasher
{
    public string Hash(string sessionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionKey);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sessionKey));
        return Convert.ToHexStringLower(bytes);
    }
}
