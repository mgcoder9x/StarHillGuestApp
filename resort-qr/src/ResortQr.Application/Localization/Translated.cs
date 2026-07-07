using ResortQr.SharedKernel.Entities;

namespace ResortQr.Application.Localization;

/// <summary>
/// Kết quả resolve bản dịch một mục: <see cref="Value"/> (null nếu thiếu), ngôn ngữ đã chọn,
/// <see cref="IsFallback"/> (dùng ngôn ngữ mặc định), <see cref="IsMissing"/> (không có cả requested lẫn default).
/// </summary>
public sealed record Translated<T>(T? Value, string ResolvedLanguage, bool IsFallback, bool IsMissing)
    where T : class, ITranslation;

/// <summary>Factory tập trung cho <see cref="Translated{T}"/> (tránh static member trên generic type — CA1000).</summary>
public static class Translated
{
    public static Translated<T> Found<T>(T value, string language) where T : class, ITranslation =>
        new(value, language, IsFallback: false, IsMissing: false);

    public static Translated<T> Fallback<T>(T value, string language) where T : class, ITranslation =>
        new(value, language, IsFallback: true, IsMissing: false);

    public static Translated<T> Missing<T>() where T : class, ITranslation =>
        new(null, string.Empty, IsFallback: false, IsMissing: true);
}
