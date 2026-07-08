namespace Bedrock.Api.Authentication;

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
/// previous) → rolling rotation không vô hiệu token đang sống. Ký (Infrastructure) và verify (Api) dùng
/// CHUNG options này qua config → chia sẻ key material KHÔNG cần project reference (giữ F14). Bind section "Jwt".
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
}
