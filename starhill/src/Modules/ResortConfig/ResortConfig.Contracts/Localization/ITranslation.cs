namespace ResortConfig.Contracts.Localization;

/// <summary>
/// Bản dịch một mục nội dung theo ngôn ngữ. Entity <c>*Translation</c> của module khác (Rules/Faq) hiện thực.
/// <see cref="HasContent"/> = nội dung chính (Title/Body/... tùy entity) sau trim KHÁC rỗng — "có row nhưng
/// rỗng" ⇒ HasContent=false (coi như thiếu, sẽ fallback). Entity tự quyết định.
/// </summary>
public interface ITranslation
{
    string LanguageCode { get; }

    bool HasContent { get; }
}
