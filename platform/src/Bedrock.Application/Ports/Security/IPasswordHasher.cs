namespace Bedrock.Application.Ports.Security;

/// <summary>
/// Băm + xác minh mật khẩu (impl Argon2id ở Infrastructure). Port bảo mật bắt buộc: KHÔNG có default,
/// thiếu → <c>RequiredPortsValidator</c> chặn boot (fail-secure, §5.5) — không bao giờ chạy với hasher no-op.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
