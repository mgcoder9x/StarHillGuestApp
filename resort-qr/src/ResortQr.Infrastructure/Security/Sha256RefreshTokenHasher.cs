using System.Security.Cryptography;
using System.Text;
using ResortQr.Application.Identity;

namespace ResortQr.Infrastructure.Security;

/// <summary>
/// Băm refresh token bằng SHA-256 (hex). Hợp lý vì token có ENTROPY CAO (CSPRNG) — không cần KDF chậm
/// như mật khẩu. Tất định → lookup theo hash. Nếu DB lộ, hash không đảo ngược ra secret.
/// </summary>
public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexStringLower(bytes);
    }
}
