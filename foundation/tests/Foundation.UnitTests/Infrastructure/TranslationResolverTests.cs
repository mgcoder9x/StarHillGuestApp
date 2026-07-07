using System;
using System.Collections.Generic;
using Foundation.Infrastructure.Localization;
using Foundation.SharedKernel.Entities;
using Xunit;

namespace Foundation.UnitTests.Infrastructure;

public sealed class TranslationResolverTests
{
    private sealed record FakeTranslation(string LanguageCode, string Body) : ITranslation
    {
        public bool HasContent => !string.IsNullOrWhiteSpace(Body);
    }

    private static readonly string[] Enabled = ["en", "vi", "ko", "zh"];
    private readonly TranslationResolver _sut = new();

    [Theory] // §2: chuẩn hóa + primary-subtag + fallback default
    [InlineData("vi", "vi")]
    [InlineData("VI", "vi")]        // không phân biệt hoa/thường
    [InlineData("ko-KR", "ko")]     // primary subtag
    [InlineData("zh-Hant", "zh")]   // phồn/giản cùng map zh (giới hạn có chủ đích)
    [InlineData("fr", "en")]        // không bật → default
    [InlineData("", "en")]          // rỗng → default
    [InlineData(null, "en")]
    public void MatchSupported_should_normalize_and_fallback(string? requested, string expected)
    {
        Assert.Equal(expected, _sut.MatchSupported(requested, Enabled, "en"));
    }

    [Fact] // Req 9.2: có bản dịch requested không rỗng → không fallback
    public void Resolve_should_return_exact_when_present()
    {
        var translations = new[] { new FakeTranslation("vi", "Xin chào"), new FakeTranslation("en", "Hello") };

        var result = _sut.Resolve(translations, "vi", "en");

        Assert.False(result.IsFallback);
        Assert.False(result.IsMissing);
        Assert.Equal("Xin chào", result.Value!.Body);
    }

    [Fact] // Req 9.3: thiếu requested → fallback default + IsFallback=true
    public void Resolve_should_fallback_to_default_when_requested_missing()
    {
        var translations = new[] { new FakeTranslation("en", "Hello") };

        var result = _sut.Resolve(translations, "vi", "en");

        Assert.True(result.IsFallback);
        Assert.Equal("Hello", result.Value!.Body);
    }

    [Fact] // Req 9.3 (điểm tinh vi): "có row nhưng rỗng" = coi như thiếu → fallback
    public void Resolve_should_treat_empty_content_as_missing()
    {
        var translations = new[] { new FakeTranslation("vi", "   "), new FakeTranslation("en", "Hello") };

        var result = _sut.Resolve(translations, "vi", "en");

        Assert.True(result.IsFallback);
        Assert.Equal("Hello", result.Value!.Body);
    }

    [Fact] // Req 9.6: thiếu cả requested lẫn default → missing, không ném
    public void Resolve_should_return_missing_when_none()
    {
        var translations = new[] { new FakeTranslation("ko", "안녕") };

        var result = _sut.Resolve(translations, "vi", "en");

        Assert.True(result.IsMissing);
        Assert.Null(result.Value);
    }

    [Fact] // Req 8.7: liệt kê mã còn thiếu bản dịch không rỗng
    public void MissingLanguages_should_list_codes_without_content()
    {
        var translations = new[] { new FakeTranslation("en", "Hello"), new FakeTranslation("vi", ""), new FakeTranslation("ko", "안녕") };

        var missing = _sut.MissingLanguages(translations, Enabled);

        Assert.Contains("vi", missing); // có row nhưng rỗng
        Assert.Contains("zh", missing); // không có row
        Assert.DoesNotContain("en", missing);
        Assert.DoesNotContain("ko", missing);
    }
}
