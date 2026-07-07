using ResortQr.Application.Abstractions;

namespace ResortQr.Infrastructure.Security;

/// <summary>
/// Làm sạch HTML theo allowlist bằng <c>Ganss.Xss</c> (không tự viết — parser HTML đúng chuẩn).
/// Base: dùng allowlist mặc định của Ganss (đã loại script/on*/javascript:) + TẮT <c>img</c> mặc định (DEC-018);
/// app có thể mở rộng allowlist khi cần (TK-028). Nội dung rỗng/null → chuỗi rỗng chuẩn hóa (Req 12.6).
/// Cấu hình đặt MỘT lần trong constructor, sau đó chỉ gọi Sanitize (dùng như singleton).
/// </summary>
public sealed class HtmlSanitizerAdapter : IHtmlSanitizer
{
    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

    public HtmlSanitizerAdapter()
    {
        _sanitizer = new Ganss.Xss.HtmlSanitizer();
        _sanitizer.AllowedTags.Remove("img"); // base tắt ảnh; bật lại có kiểm soát ở app nếu cần
    }

    public string Sanitize(string? rawHtml) =>
        string.IsNullOrEmpty(rawHtml) ? string.Empty : _sanitizer.Sanitize(rawHtml);
}
