using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.GuestAccess;

/// <summary>
/// Băm khóa phiên khách (cookie thiết bị). DB lưu HASH, cookie giữ raw (04 §7.1). Token entropy cao (CSPRNG)
/// → SHA-256 là đủ (không cần KDF chậm). Tách khỏi <c>IRefreshTokenHasher</c> cho rõ ngữ nghĩa.
/// </summary>
public interface IGuestSessionKeyHasher : ISingletonService
{
    string Hash(string sessionKey);
}
