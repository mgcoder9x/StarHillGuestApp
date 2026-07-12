using ResortConfig.Contracts.Localization;

namespace ResortConfig.Application.Localization;

/// <summary>
/// Hiện thực <see cref="ITranslationResolver"/> — thuật toán fallback thuần (port nguyên logic từ resort-qr
/// <c>ResortQr.Infrastructure.Localization.TranslationResolver</c>). So khớp mã ngôn ngữ OrdinalIgnoreCase
/// (không ToLower → tránh CA1308). Đăng ký singleton thủ công ở <c>AddResortConfigInfrastructure</c> (QR-DV-002).
/// </summary>
public sealed class TranslationResolver : ITranslationResolver
{
    public string MatchSupported(string? requested, IReadOnlyCollection<string> enabledCodes, string defaultCode)
    {
        ArgumentNullException.ThrowIfNull(enabledCodes);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCode);

        if (string.IsNullOrWhiteSpace(requested))
        {
            return defaultCode;
        }

        var trimmed = requested.Trim();

        var exact = enabledCodes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        var dashIndex = trimmed.IndexOf('-', StringComparison.Ordinal);
        var primary = dashIndex >= 0 ? trimmed[..dashIndex] : trimmed;

        var primaryMatch = enabledCodes.FirstOrDefault(c => string.Equals(c, primary, StringComparison.OrdinalIgnoreCase));
        return primaryMatch ?? defaultCode;
    }

    public Translated<T> Resolve<T>(IReadOnlyCollection<T> translations, string requestedLanguage, string defaultLanguage)
        where T : class, ITranslation
    {
        ArgumentNullException.ThrowIfNull(translations);

        var exact = translations.FirstOrDefault(t => Matches(t.LanguageCode, requestedLanguage) && t.HasContent);
        if (exact is not null)
        {
            return Translated.Found(exact, requestedLanguage);
        }

        var fallback = translations.FirstOrDefault(t => Matches(t.LanguageCode, defaultLanguage) && t.HasContent);
        if (fallback is not null)
        {
            return Translated.Fallback(fallback, defaultLanguage);
        }

        return Translated.Missing<T>();
    }

    public IReadOnlyList<string> MissingLanguages<T>(IReadOnlyCollection<T> translations, IReadOnlyCollection<string> enabledCodes)
        where T : class, ITranslation
    {
        ArgumentNullException.ThrowIfNull(translations);
        ArgumentNullException.ThrowIfNull(enabledCodes);

        var present = translations
            .Where(t => t.HasContent)
            .Select(t => t.LanguageCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return enabledCodes.Where(code => !present.Contains(code)).ToList();
    }

    private static bool Matches(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
