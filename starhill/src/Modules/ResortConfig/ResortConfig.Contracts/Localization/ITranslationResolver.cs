namespace ResortConfig.Contracts.Localization;

/// <summary>
/// Resolve nội dung đa ngôn ngữ với fallback (Req 2, Req 8.7). Thuật toán THUẦN (không I/O) → test được ở
/// mọi máy. KHÔNG kế thừa marker DI (QR-DV-002 — Contracts không ref Bedrock.Application); impl đăng ký
/// thủ công singleton ở <c>AddResortConfigInfrastructure</c>. Module khác dùng qua assembly Contracts này.
/// </summary>
public interface ITranslationResolver
{
    /// <summary>
    /// Chuẩn hóa + so khớp mã ngôn ngữ (BCP-47): khớp chính xác → primary-subtag (`ko-KR`→`ko`) → default.
    /// KHÔNG phân biệt hoa/thường. Trả mã (theo enabledCodes) dùng để hiển thị (Req 2.2).
    /// </summary>
    string MatchSupported(string? requested, IReadOnlyCollection<string> enabledCodes, string defaultCode);

    /// <summary>
    /// Resolve bản dịch của MỘT mục: requested (không rỗng) → default (không rỗng, IsFallback) → missing.
    /// "Có row nhưng rỗng" coi như thiếu (Req 2.5).
    /// </summary>
    Translated<T> Resolve<T>(IReadOnlyCollection<T> translations, string requestedLanguage, string defaultLanguage)
        where T : class, ITranslation;

    /// <summary>Các mã (trong enabledCodes) CHƯA có bản dịch không rỗng — cho admin editor (Req 8.7).</summary>
    IReadOnlyList<string> MissingLanguages<T>(IReadOnlyCollection<T> translations, IReadOnlyCollection<string> enabledCodes)
        where T : class, ITranslation;
}
