using System.Security.Cryptography;
using System.Text;
using ResortQr.Application.GuestAccess;

namespace ResortQr.Infrastructure.Security;

/// <summary>
/// Băm khóa phiên khách bằng SHA-256 (hex, 64 ký tự — khớp cột session_key_hash). Token entropy cao (CSPRNG)
/// nên SHA-256 là đủ; tất định để lookup theo hash. DB lưu HASH, cookie giữ raw. Auto-đăng ký (ISingletonService).
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
