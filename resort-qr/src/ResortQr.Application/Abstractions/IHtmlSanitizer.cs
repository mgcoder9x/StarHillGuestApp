using ResortQr.SharedKernel.DependencyInjection;

namespace ResortQr.Application.Abstractions;

/// <summary>
/// Làm sạch HTML theo allowlist (chống XSS) trước khi LƯU. Nội dung rỗng/null → trả chuỗi rỗng chuẩn hóa.
/// </summary>
public interface IHtmlSanitizer : ISingletonService
{
    string Sanitize(string? rawHtml);
}
