using System.ComponentModel.DataAnnotations;

namespace ResortQr.Infrastructure.Security;

/// <summary>
/// Cấu hình JWT. <see cref="SigningKey"/> phải ≥ 32 byte (256-bit) cho HS256 (kiểm ở JwtOptionsValidator);
/// đến từ secret store, KHÔNG commit. Validate-on-startup ở tầng Api (fail-fast nếu thiếu ở Production).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Tuổi thọ access token (phút). Mặc định 15 (khoảng khuyến nghị 5–60).</summary>
    [Range(5, 60)]
    public int AccessTokenMinutes { get; set; } = 15;
}
