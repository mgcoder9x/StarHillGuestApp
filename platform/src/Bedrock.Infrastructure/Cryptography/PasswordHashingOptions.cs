namespace Bedrock.Infrastructure.Cryptography;

/// <summary>
/// Tham số Argon2id. Default theo khuyến nghị OWASP (m=19 MiB, t=2, p=1) — PHẢI benchmark + tune theo phần
/// cứng prod để 1 lần hash ~ vài trăm ms. Hash lưu dạng PHC string tự-mô-tả → nâng tham số sau vẫn verify
/// được hash cũ (rehash-on-login khi cần). Bind từ config section <see cref="SectionName"/>.
/// </summary>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    /// <summary>Bộ nhớ (KiB). 19456 KiB = 19 MiB.</summary>
    public int MemoryKib { get; set; } = 19456;

    /// <summary>Số vòng lặp (time cost).</summary>
    public int Iterations { get; set; } = 2;

    /// <summary>Mức song song (lanes).</summary>
    public int DegreeOfParallelism { get; set; } = 1;

    /// <summary>Độ dài salt (byte), ≥ 16.</summary>
    public int SaltSize { get; set; } = 16;

    /// <summary>Độ dài hash (byte), ≥ 32.</summary>
    public int HashSize { get; set; } = 32;

    internal const int MinMemoryKib = 8 * 1024;
    internal const int MaxMemoryKib = 1024 * 1024;
    internal const int MaxIterations = 20;
    internal const int MaxParallelism = 32;
    internal const int MinSaltSize = 16;
    internal const int MaxSaltSize = 64;
    internal const int MinHashSize = 32;
    internal const int MaxHashSize = 128;

    public static void Validate(PasswordHashingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!IsWithinBounds(
                options.MemoryKib,
                options.Iterations,
                options.DegreeOfParallelism,
                options.SaltSize,
                options.HashSize))
        {
            throw new InvalidOperationException(
                "PasswordHashing options vượt policy an toàn: memory 8MiB..1GiB, iterations 1..20, "
                + "parallelism 1..32, salt 16..64 byte, hash 32..128 byte.");
        }
    }

    internal static bool IsWithinBounds(int memory, int iterations, int parallelism, int saltSize, int hashSize) =>
        memory is >= MinMemoryKib and <= MaxMemoryKib
        && iterations is >= 1 and <= MaxIterations
        && parallelism is >= 1 and <= MaxParallelism
        && saltSize is >= MinSaltSize and <= MaxSaltSize
        && hashSize is >= MinHashSize and <= MaxHashSize;
}
