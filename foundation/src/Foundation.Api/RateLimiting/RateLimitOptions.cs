using System.ComponentModel.DataAnnotations;

namespace Foundation.Api.RateLimiting;

/// <summary>
/// Cấu hình rate limit generic (fixed window theo IP). Default rộng rãi (chống brute-force, không cản dùng thường).
/// Resort thêm policy riêng (resolve/guest-write partition theo GuestSessionId) trên nền này.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}
