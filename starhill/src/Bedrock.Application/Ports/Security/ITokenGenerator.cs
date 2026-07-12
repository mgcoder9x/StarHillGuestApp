namespace Bedrock.Application.Ports.Security;

/// <summary>
/// Sinh token ngẫu nhiên an toàn (CSPRNG) dạng base64url (URL-safe, không padding).
/// Dùng cho refresh-token secret/public-token... KHÔNG dùng <c>System.Random</c>.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>Sinh chuỗi base64url từ <paramref name="byteLength"/> byte entropy (mặc định 32 = 256-bit).</summary>
    string NewToken(int byteLength = 32);
}
