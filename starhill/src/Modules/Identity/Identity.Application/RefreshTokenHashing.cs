using System.Security.Cryptography;
using System.Text;

namespace Identity.Application;

/// <summary>
/// Hash refresh-token bằng SHA-256 hex thường (design §7.4). Refresh token là chuỗi CSPRNG 256-bit entropy cao →
/// SHA-256 tất định LÀ ĐỦ (KHÔNG cần Argon2 — Argon2 dành cho password entropy thấp). NGUỒN DUY NHẤT của phép hash
/// refresh-token (dùng CHUNG cho login phát family + refresh rotation) → không lệch thuật toán giữa hai đường
/// (login lưu hash / refresh tra-cứu-theo hash phải KHỚP). Primitive chuẩn, không phải "công nghệ swap được".
/// </summary>
internal static class RefreshTokenHashing
{
    public static string Sha256Hex(string raw) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
