using Foundation.SharedKernel.DependencyInjection;

namespace Foundation.Application.Abstractions;

/// <summary>
/// Sinh token ngẫu nhiên an toàn (CSPRNG) dạng base64url (URL-safe, không padding).
/// Dùng cho PublicToken/refresh-token secret... KHÔNG dùng <c>System.Random</c>.
/// </summary>
public interface ITokenGenerator : ISingletonService
{
    /// <summary>Sinh chuỗi base64url từ <paramref name="byteLength"/> byte entropy (mặc định 32 = 256-bit).</summary>
    string NewToken(int byteLength = 32);
}
