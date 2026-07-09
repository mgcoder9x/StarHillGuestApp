using System.Security.Cryptography;
using Bedrock.Application.Ports.Security;

namespace Bedrock.Infrastructure.Tokens;

/// <summary>
/// Impl <see cref="ITokenGenerator"/> bằng CSPRNG (<see cref="RandomNumberGenerator"/>) → base64url (URL-safe,
/// không padding). 32 byte (256-bit) → 43 ký tự. KHÔNG dùng <see cref="System.Random"/> (không an toàn mật mã).
/// Port bảo mật bắt buộc (§5.5) — không default no-op.
/// </summary>
public sealed class CryptoTokenGenerator : ITokenGenerator
{
    private const int MinBytes = 16; // 128-bit sàn — dưới mức này không đủ entropy chống đoán.

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
