using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bedrock.Application.Ports.Security;
using Konscious.Security.Cryptography;

namespace Bedrock.Infrastructure.Cryptography;

/// <summary>
/// Impl <see cref="IPasswordHasher"/> bằng Argon2id — lưu chuỗi PHC tự-mô-tả
/// <c>$argon2id$v=19$m=..,t=..,p=..$salt$hash</c> (tham số nằm trong hash → nâng cost sau vẫn verify hash cũ).
/// Verify hằng-thời-gian (<see cref="CryptographicOperations.FixedTimeEquals"/>) chống timing attack. KHÔNG bao
/// giờ lưu/log mật khẩu thô; hash lỗi định dạng → trả false (KHÔNG ném). Port bảo mật bắt buộc (§5.5) — không default.
/// </summary>
public sealed class Argon2idPasswordHasher(PasswordHashingOptions options) : IPasswordHasher
{
    private const int Argon2Version = 19; // Argon2 v1.3 (0x13).

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        PasswordHashingOptions.Validate(options);
        EnsurePasswordSize(password);

        var salt = RandomNumberGenerator.GetBytes(options.SaltSize);
        var hash = Compute(password, salt, options.MemoryKib, options.Iterations, options.DegreeOfParallelism, options.HashSize);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"$argon2id$v={Argon2Version}$m={options.MemoryKib},t={options.Iterations},p={options.DegreeOfParallelism}${ToB64(salt)}${ToB64(hash)}");
    }

    public bool Verify(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);
        if (Encoding.UTF8.GetByteCount(password) > 4096)
        {
            return false;
        }

        try
        {
            if (!TryParse(hash, out var parsed))
            {
                return false;
            }

            if (!PasswordHashingOptions.IsWithinBounds(
                    parsed.Memory,
                    parsed.Iterations,
                    parsed.Parallelism,
                    parsed.Salt.Length,
                    parsed.Hash.Length))
            {
                return false;
            }

            var computed = Compute(password, parsed.Salt, parsed.Memory, parsed.Iterations, parsed.Parallelism, parsed.Hash.Length);
            return CryptographicOperations.FixedTimeEquals(computed, parsed.Hash);
        }
        catch (FormatException)
        {
            return false; // hash không đúng định dạng PHC/base64 → coi như verify thất bại (không ném).
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKib, int iterations, int parallelism, int hashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKib,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(hashSize);
    }

    private static string ToB64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

    private static byte[] FromB64(string value)
    {
        var padded = (value.Length % 4) switch
        {
            2 => value + "==",
            3 => value + "=",
            0 => value,
            _ => throw new FormatException("Chuỗi base64 không hợp lệ."),
        };
        return Convert.FromBase64String(padded);
    }

    private static bool TryParse(string phc, out ParsedHash parsed)
    {
        parsed = default;

        // $argon2id$v=19$m=..,t=..,p=..$<salt>$<hash> → split '$' cho 6 phần (phần đầu rỗng).
        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[0].Length != 0 || parts[1] != "argon2id")
        {
            return false;
        }

        if (!string.Equals(parts[2], $"v={Argon2Version}", StringComparison.Ordinal))
        {
            return false;
        }

        var perf = parts[3].Split(',');
        if (perf.Length != 3)
        {
            return false;
        }

        var memory = ParseTagged(perf[0], "m=");
        var iterations = ParseTagged(perf[1], "t=");
        var parallelism = ParseTagged(perf[2], "p=");
        if (memory is null || iterations is null || parallelism is null)
        {
            return false;
        }

        parsed = new ParsedHash(memory.Value, iterations.Value, parallelism.Value, FromB64(parts[4]), FromB64(parts[5]));
        return true;
    }

    private static int? ParseTagged(string segment, string tag)
    {
        if (!segment.StartsWith(tag, StringComparison.Ordinal))
        {
            return null;
        }

        return int.TryParse(segment.AsSpan(tag.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static void EnsurePasswordSize(string password)
    {
        if (Encoding.UTF8.GetByteCount(password) > 4096)
        {
            throw new ArgumentException("Password UTF-8 length must not exceed 4096 bytes.", nameof(password));
        }
    }

    private readonly record struct ParsedHash(int Memory, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
}
