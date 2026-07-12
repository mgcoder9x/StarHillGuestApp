using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using Xunit;

namespace ResortConfig.UnitTests;

/// <summary>
/// GUARD Correctness Property 5 (toàn vẹn bản dịch/fallback) + Req 2.2 (match ngôn ngữ). Thuần, không DB →
/// chạy mọi máy. Kiểm: match exact/primary-subtag/default; Resolve found→fallback(IsFallback)→missing
/// (row rỗng coi như thiếu); MissingLanguages.
/// </summary>
public sealed class TranslationResolverTests
{
    private static readonly string[] Enabled = ["en", "vi", "ko", "zh"];
    private readonly TranslationResolver _sut = new();

    /// <summary>Bản dịch giả cho test — HasContent do test quyết định (mô phỏng "row rỗng").</summary>
    private sealed record FakeTranslation(string LanguageCode, bool HasContent) : ITranslation;

    [Theory]
    [InlineData("vi", "vi")]      // khớp chính xác
    [InlineData("VI", "vi")]      // không phân biệt hoa/thường
    [InlineData("ko-KR", "ko")]   // primary-subtag
    [InlineData("fr", "en")]      // không hỗ trợ → default
    [InlineData("", "en")]        // rỗng → default
    [InlineData(null, "en")]      // null → default
    public void MatchSupported_resolves_expected(string? requested, string expected)
    {
        var result = _sut.MatchSupported(requested, Enabled, defaultCode: "en");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_returns_requested_when_present_and_has_content()
    {
        var translations = new[]
        {
            new FakeTranslation("en", HasContent: true),
            new FakeTranslation("vi", HasContent: true),
        };

        var result = _sut.Resolve(translations, requestedLanguage: "vi", defaultLanguage: "en");

        Assert.False(result.IsMissing);
        Assert.False(result.IsFallback);
        Assert.Equal("vi", result.ResolvedLanguage);
        Assert.Equal("vi", result.Value!.LanguageCode);
    }

    [Fact]
    public void Resolve_falls_back_to_default_and_flags_isfallback()
    {
        var translations = new[]
        {
            new FakeTranslation("en", HasContent: true),
            new FakeTranslation("vi", HasContent: false), // row rỗng ⇒ coi như thiếu
        };

        var result = _sut.Resolve(translations, requestedLanguage: "vi", defaultLanguage: "en");

        Assert.False(result.IsMissing);
        Assert.True(result.IsFallback);
        Assert.Equal("en", result.ResolvedLanguage);
    }

    [Fact]
    public void Resolve_is_missing_when_neither_requested_nor_default_has_content()
    {
        var translations = new[]
        {
            new FakeTranslation("en", HasContent: false),
            new FakeTranslation("vi", HasContent: false),
        };

        var result = _sut.Resolve(translations, requestedLanguage: "vi", defaultLanguage: "en");

        Assert.True(result.IsMissing);
        Assert.Null(result.Value);
    }

    [Fact]
    public void MissingLanguages_lists_enabled_codes_without_content()
    {
        var translations = new[]
        {
            new FakeTranslation("en", HasContent: true),
            new FakeTranslation("vi", HasContent: true),
            new FakeTranslation("ko", HasContent: false), // rỗng ⇒ thiếu
        };

        var missing = _sut.MissingLanguages(translations, Enabled);

        Assert.Equal(["ko", "zh"], missing);
    }
}
