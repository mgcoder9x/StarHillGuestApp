using System.Security.Cryptography;
using Foundation.Application.Abstractions;

namespace Foundation.Infrastructure.Security;

/// <summary>
/// Sinh token bằng CSPRNG (<see cref="RandomNumberGenerator"/>) → base64url (URL-safe, không padding).
/// 32 byte (256-bit) → 43 ký tự. KHÔNG dùng <c>System.Random</c> (không an toàn mật mã).
/// </summary>
public sealed class CryptoTokenGenerator : ITokenGenerator
{
    private const int MinBytes = 16;

    public string NewToken(int byteLength = 32)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLength, MinBytes);

        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
