using StarHill.Html;
using Xunit;

namespace StarHill.Html.Tests;

/// <summary>
/// Guard adapter sanitize (QR-AD-031/CP12/Req 8.6): loại script/event-handler/javascript-uri/img mặc định; giữ
/// định dạng cơ bản; null/rỗng → rỗng. Verify tích hợp Ganss.Xss 9.0.892 chạy đúng trên .NET 10 (khử rủi ro package/version).
/// </summary>
public sealed class GanssHtmlSanitizerAdapterTests
{
    private readonly GanssHtmlSanitizerAdapter _sut = new();

    [Fact]
    public void Removes_script_tag()
    {
        var result = _sut.Sanitize("<p>hi</p><script>alert(1)</script>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hi", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Removes_event_handler_attributes()
    {
        var result = _sut.Sanitize("<a href=\"/x\" onclick=\"steal()\">link</a>");

        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("link", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Removes_javascript_uri()
    {
        var result = _sut.Sanitize("<a href=\"javascript:alert(1)\">x</a>");

        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strips_img_by_default()
    {
        var result = _sut.Sanitize("<p>a</p><img src=\"x\" onerror=\"y\">");

        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Keeps_basic_formatting()
    {
        var result = _sut.Sanitize("<p><b>bold</b> and <em>italic</em></p>");

        Assert.Contains("<b>bold</b>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<em>italic</em>", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    public void Returns_empty_for_empty(string input)
    {
        Assert.Equal(string.Empty, _sut.Sanitize(input));
    }
}
