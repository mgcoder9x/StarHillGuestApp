namespace Bedrock.Application.Ports.Html;

/// <summary>
/// Làm sạch HTML theo allowlist (chống XSS) trước khi LƯU. Nội dung rỗng/null → trả chuỗi rỗng chuẩn hóa.
/// Đặt ở namespace riêng <c>Ports.Html</c> (AD-021) — mối quan tâm "làm sạch nội dung", tách khỏi
/// <c>Ports.Security</c> (auth/crypto). Port bảo mật bắt buộc: không có default, thiếu → chặn boot (§5.5).
/// </summary>
public interface IHtmlSanitizer
{
    string Sanitize(string html);
}
