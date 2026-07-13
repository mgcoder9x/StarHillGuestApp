namespace ResortConfig.Domain;

/// <summary>
/// P1(b) — BẤT BIẾN MIỀN của tập ngôn ngữ MỘT resort: <b>đúng MỘT</b> ngôn ngữ mặc định (<see cref="ResortLanguage.IsDefault"/>)
/// và ngôn ngữ mặc định đó phải đang BẬT (<see cref="ResortLanguage.IsEnabled"/>). Tách thành hàm THUẦN (không I/O) để:
/// (a) thao tác đổi-mặc-định áp NGUYÊN TỬ trên tập đã nạp rồi lưu một lần; (b) seeder fail-fast nếu dữ liệu seed sai;
/// (c) unit-test đầy đủ KHÔNG cần DB.
/// <para>
/// <b>Vì sao miền phải giữ bất biến (không chỉ dựa DB):</b> chỉ số partial-unique <c>ux_lang_default</c> chỉ chặn
/// "NHIỀU HƠN một" default và CHỈ có trên Npgsql (provider khác/test không có). Nó KHÔNG chặn "KHÔNG có default" —
/// mà resort 0-default làm <c>TranslationResolver</c> mất <c>defaultCode</c> → i18n gãy. Do đó "đúng-một + default-enabled"
/// là bất biến nghiệp vụ, phải do miền enforce.
/// </para>
/// </summary>
public static class ResortLanguagePolicy
{
    /// <summary>Đúng MỘT ngôn ngữ có <see cref="ResortLanguage.IsDefault"/> = true trong tập.</summary>
    public static bool HasExactlyOneDefault(IReadOnlyCollection<ResortLanguage> languages)
    {
        ArgumentNullException.ThrowIfNull(languages);
        return languages.Count(l => l.IsDefault) == 1;
    }

    /// <summary>Bất biến ĐẦY ĐỦ: đúng một default VÀ default đó đang bật (default bị tắt = vô nghĩa cho fallback i18n).</summary>
    public static bool SatisfiesInvariant(IReadOnlyCollection<ResortLanguage> languages)
    {
        ArgumentNullException.ThrowIfNull(languages);
        if (languages.Count(l => l.IsDefault) != 1)
        {
            return false;
        }

        return languages.Single(l => l.IsDefault).IsEnabled;
    }

    /// <summary>
    /// Đặt ngôn ngữ mặc định NGUYÊN TỬ (in-memory) trên tập của MỘT resort: <paramref name="targetCode"/> (so khớp
    /// <see cref="ResortLanguage.Code"/> KHÔNG phân biệt hoa/thường) phải TỒN TẠI và đang BẬT. Hợp lệ → set target
    /// <see cref="ResortLanguage.IsDefault"/>=true và MỌI ngôn ngữ khác =false trong CÙNG một lượt (bất biến giữ ở
    /// mọi thời điểm nhìn từ tầng miền), trả <c>true</c>. Không hợp lệ (không tồn tại/không bật) → KHÔNG đổi gì, trả
    /// <c>false</c> (giữ nguyên bất biến cũ). Caller (use case) sau đó lưu MỘT lần → nguyên tử ở DB.
    /// </summary>
    public static bool TrySetDefault(IReadOnlyCollection<ResortLanguage> languages, string targetCode)
    {
        ArgumentNullException.ThrowIfNull(languages);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCode);

        var target = languages.FirstOrDefault(l =>
            string.Equals(l.Code, targetCode, StringComparison.OrdinalIgnoreCase));
        if (target is null || !target.IsEnabled)
        {
            return false; // target không tồn tại hoặc bị tắt → không đổi (không tạo trạng thái 0-default/ default-tắt).
        }

        foreach (var language in languages)
        {
            language.IsDefault = ReferenceEquals(language, target);
        }

        return true;
    }
}
