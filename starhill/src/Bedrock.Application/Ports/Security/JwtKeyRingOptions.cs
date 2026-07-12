namespace Bedrock.Application.Ports.Security;

/// <summary>
/// Một khóa ký JWT trong key-ring (F22). <see cref="Kid"/> gắn vào header token; <see cref="Secret"/> là
/// khóa đối xứng HS256 dạng base64.
/// </summary>
public sealed class JwtSigningKey
{
    public string Kid { get; set; } = string.Empty;

    /// <summary>Khóa đối xứng base64 (≥256-bit cho HS256). Là SECRET — nạp qua secret store, không commit (F35).</summary>
    public string Secret { get; set; } = string.Empty;
}

/// <summary>
/// Key-ring JWT (F22): ký bằng <see cref="ActiveKid"/>, verify bằng TẤT CẢ <see cref="Keys"/> (active +
/// previous) → rolling rotation không vô hiệu token đang sống. Ký (<c>Bedrock.Infrastructure</c>) và verify
/// (<c>Bedrock.Api</c>) dùng CHUNG type này — đặt ở <c>Bedrock.Application</c> (phụ thuộc chung của cả hai) để
/// chia sẻ KHÔNG cần Api↔Infrastructure reference (giữ F14). Hai layer bind CÙNG section "Jwt" từ config
/// (validate-on-start — F35).
/// </summary>
public sealed class JwtKeyRingOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Kid của khóa đang dùng để KÝ (phải có trong <see cref="Keys"/>).</summary>
    public string ActiveKid { get; set; } = string.Empty;

    /// <summary>Toàn bộ khóa còn hiệu lực để VERIFY (get-only để binding populate — tránh CA1819).</summary>
    public IList<JwtSigningKey> Keys { get; } = new List<JwtSigningKey>();

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Tuổi thọ access-token (phút). Mặc định 15 (khuyến nghị 5–60); phía ký đặt <c>exp</c> từ đây.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
}
