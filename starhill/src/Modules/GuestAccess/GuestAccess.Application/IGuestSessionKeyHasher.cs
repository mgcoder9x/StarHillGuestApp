namespace GuestAccess.Application;

/// <summary>
/// Băm khóa phiên khách (cookie thiết bị). DB CHỈ lưu HASH, cookie giữ raw (QR-AD-025). Raw key có entropy cao
/// (CSPRNG 256-bit từ <c>ITokenGenerator</c>) → SHA-256 tất định là đủ để lookup, KHÔNG cần KDF chậm. Port thuần
/// (không marker DI — đăng ký thủ công singleton ở Infrastructure, mirror QR-DV-002).
/// </summary>
public interface IGuestSessionKeyHasher
{
    string Hash(string sessionKey);
}
