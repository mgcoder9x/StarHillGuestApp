using System.ComponentModel.DataAnnotations;

namespace ResortQr.Infrastructure.Security;

/// <summary>
/// Tham số Argon2id. Default theo một cấu hình OWASP (m=19 MiB, t=2, p=1) — nhưng PHẢI tune + benchmark
/// theo phần cứng prod để 1 lần hash ~ vài trăm ms (TK-026). Lưu PHC string → rehash-on-login khi nâng tham số.
/// </summary>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    /// <summary>Bộ nhớ (KiB). 19456 KiB = 19 MiB.</summary>
    [Range(1024, int.MaxValue)]
    public int MemoryKib { get; set; } = 19456;

    [Range(1, int.MaxValue)]
    public int Iterations { get; set; } = 2;

    [Range(1, int.MaxValue)]
    public int DegreeOfParallelism { get; set; } = 1;

    /// <summary>Độ dài salt (byte), ≥ 16.</summary>
    [Range(16, int.MaxValue)]
    public int SaltSize { get; set; } = 16;

    /// <summary>Độ dài hash (byte), ≥ 32.</summary>
    [Range(32, int.MaxValue)]
    public int HashSize { get; set; } = 32;
}
