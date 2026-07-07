using ResortQr.SharedKernel.DependencyInjection;
using ResortQr.SharedKernel.Entities;

namespace ResortQr.Application.Localization;

/// <summary>
/// Resolve nội dung đa ngôn ngữ với fallback (Req 9, thiết kế 18). Thuật toán thuần (không I/O) → test được.
/// </summary>
public interface ITranslationResolver : ISingletonService
{
    /// <summary>
    /// Chuẩn hóa + so khớp mã ngôn ngữ (BCP-47): khớp chính xác → primary-subtag (`ko-KR`→`ko`) → default.
    /// KHÔNG phân biệt hoa/thường. Trả mã ngôn ngữ (theo enabledCodes) sẽ dùng để hiển thị (Req 2.2).
    /// </summary>
    string MatchSupported(string? requested, IReadOnlyCollection<string> enabledCodes, string defaultCode);

    /// <summary>
    /// Resolve bản dịch của MỘT mục: requested (không rỗng) → default (không rỗng, IsFallback) → missing.
    /// "Có row nhưng rỗng" coi như thiếu (Req 9.2/9.3/9.6).
    /// </summary>
    Translated<T> Resolve<T>(IReadOnlyCollection<T> translations, string requestedLanguage, string defaultLanguage)
        where T : class, ITranslation;

    /// <summary>Các mã (trong enabledCodes) CHƯA có bản dịch không rỗng — cho admin editor (Req 8.7).</summary>
    IReadOnlyList<string> MissingLanguages<T>(IReadOnlyCollection<T> translations, IReadOnlyCollection<string> enabledCodes)
        where T : class, ITranslation;
}
