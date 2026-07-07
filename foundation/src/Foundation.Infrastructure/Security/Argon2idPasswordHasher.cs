using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Foundation.Application.Abstractions.Security;
using Konscious.Security.Cryptography;

namespace Foundation.Infrastructure.Security;

/// <summary>
/// Hash mật khẩu Argon2id, lưu chuỗi PHC tự mô tả: <c>$argon2id$v=19$m=..,t=..,p=..$salt$hash</c>.
/// Verify hằng-thời-gian (<see cref="CryptographicOperations.FixedTimeEquals"/>); phát hiện cần rehash
/// khi tham số đã lưu khác cấu hình hiện tại. KHÔNG bao giờ lưu/log mật khẩu thô. Không throw khi hash hỏng.
/// </summary>
public sealed class Argon2idPasswordHasher : IPasswordHasher
{
    private const int Version = 19; // Argon2 v1.3 (0x13)
    private readonly PasswordHashingOptions _options;

    public Argon2idPasswordHasher(PasswordHashingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(_options.SaltSize);
        var hash = Compute(password, salt, _options.MemoryKib, _options.Iterations, _options.DegreeOfParallelism, _options.HashSize);

        return string.Create(CultureInfo.InvariantCulture,
            $"$argon2id$v={Version}$m={_options.MemoryKib},t={_options.Iterations},p={_options.DegreeOfParallelism}${ToB64(salt)}${ToB64(hash)}");
    }

    public PasswordVerificationResult Verify(string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        try
        {
            if (!TryParse(hashedPassword, out var parsed))
            {
                return PasswordVerificationResult.Failed;
            }

            var computed = Compute(providedPassword, parsed.Salt, parsed.Memory, parsed.Iterations, parsed.Parallelism, parsed.Hash.Length);

            if (!CryptographicOperations.FixedTimeEquals(computed, parsed.Hash))
            {
                return PasswordVerificationResult.Failed;
            }

            var needsRehash =
                parsed.Memory != _options.MemoryKib ||
                parsed.Iterations != _options.Iterations ||
                parsed.Parallelism != _options.DegreeOfParallelism ||
                parsed.Hash.Length != _options.HashSize;

            return needsRehash ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
        catch (ArgumentException)
        {
            return PasswordVerificationResult.Failed;
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

        // $argon2id$v=19$m=..,t=..,p=..$<salt>$<hash>  → split '$' cho 6 phần (phần đầu rỗng).
        var parts = phc.Split('$');
        if (parts.Length != 6 || parts[0].Length != 0 || parts[1] != "argon2id")
        {
            return false;
        }

        if (!parts[2].StartsWith("v=", StringComparison.Ordinal))
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

    private readonly record struct ParsedHash(int Memory, int Iterations, int Parallelism, byte[] Salt, byte[] Hash);
}
