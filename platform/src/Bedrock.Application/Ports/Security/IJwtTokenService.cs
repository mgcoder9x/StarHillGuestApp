using System.Security.Claims;

namespace Bedrock.Application.Ports.Security;

/// <summary>
/// Phát JWT access-token (F22 — key-ring): ký bằng khóa <c>ActiveKid</c> và gắn <c>kid</c> vào header để
/// phía verify (<c>Bedrock.Api</c>) chọn đúng khóa. Port THUẦN: nhận sẵn <see cref="ClaimsIdentity"/> do
/// module nghiệp vụ dựng (business claims như <c>sub</c>/<c>role</c>/<c>permission</c>/<c>tenant_id</c>/<c>sid</c>
/// — AD-023); service chỉ bổ sung claim đăng ký chuẩn (<c>iss</c>/<c>aud</c>/<c>iat</c>/<c>nbf</c>/<c>exp</c>).
/// Impl (Argon2/JWT) ở Infrastructure; ký &amp; verify gặp nhau ở CONFIG (<c>JwtKeyRingOptions</c>), KHÔNG ở
/// project reference → giữ F14 (Api ⊥ Infrastructure).
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Ký <paramref name="identity"/> thành JWT compact (header có <c>kid</c> của active key).</summary>
    string Issue(ClaimsIdentity identity);
}
