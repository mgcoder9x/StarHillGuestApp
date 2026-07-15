using Bedrock.Application.Ports.Html;

namespace StarHill.Html;

/// <summary>
/// Adapter sản phẩm cho port bảo mật base <see cref="IHtmlSanitizer"/> — làm sạch HTML theo allowlist bằng
/// <c>Ganss.Xss</c> (parser HTML đúng chuẩn, KHÔNG tự viết). Cấu hình: allowlist mặc định của Ganss (đã loại
/// <c>script</c>/<c>on*</c>/<c>javascript:</c>) + TẮT <c>img</c> mặc định (bật lại có kiểm soát nếu cần sau).
/// Nội dung null/rỗng → chuỗi rỗng chuẩn hóa.
/// <para>
/// <b>Thread-safety (QR-N-033):</b> <c>Ganss.Xss.HtmlSanitizer</c> KHÔNG đảm bảo thread-safe cho <c>Sanitize</c>
/// đồng thời qua mọi version, nên KHÔNG chia sẻ một instance làm singleton. Adapter đăng ký singleton nhưng KHỞI
/// TẠO sanitizer MỚI mỗi lần <see cref="Sanitize"/> → an toàn concurrency tận gốc. Sanitize chỉ chạy ở đường ghi
/// nội dung của admin (tần suất thấp) nên chi phí khởi tạo không đáng kể.
/// </para>
/// </summary>
public sealed class GanssHtmlSanitizerAdapter : IHtmlSanitizer
{
    public string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var sanitizer = new Ganss.Xss.HtmlSanitizer();
        sanitizer.AllowedTags.Remove("img"); // base tắt ảnh; bật lại có kiểm soát ở app nếu cần.
        return sanitizer.Sanitize(html);
    }
}
