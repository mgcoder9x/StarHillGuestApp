using ResortQr.Infrastructure.Security;
using Xunit;

namespace ResortQr.UnitTests.Infrastructure;

public sealed class HtmlSanitizerAdapterTests
{
    private readonly HtmlSanitizerAdapter _sut = new();

    [Fact] // Req 12.4: bỏ <script>
    public void Should_remove_script_tag()
    {
        var result = _sut.Sanitize("<p>hi</p><script>alert(1)</script>");
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hi", result, StringComparison.Ordinal);
    }

    [Fact] // Req 12.4: bỏ thuộc tính sự kiện on*
    public void Should_remove_event_handler_attributes()
    {
        var result = _sut.Sanitize("<a href=\"/x\" onclick=\"steal()\">link</a>");
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("link", result, StringComparison.Ordinal);
    }

    [Fact] // Req 12.4: bỏ URI javascript:
    public void Should_remove_javascript_uri()
    {
        var result = _sut.Sanitize("<a href=\"javascript:alert(1)\">x</a>");
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // DEC-018: base tắt <img>
    public void Should_strip_img_by_default()
    {
        var result = _sut.Sanitize("<p>a</p><img src=\"x\" onerror=\"y\">");
        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] // Giữ định dạng cơ bản an toàn
    public void Should_keep_basic_formatting()
    {
        var result = _sut.Sanitize("<p><b>bold</b> and <em>italic</em></p>");
        Assert.Contains("<b>bold</b>", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<em>italic</em>", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory] // Req 12.6: rỗng/null → chuỗi rỗng chuẩn hóa, không ném
    [InlineData(null)]
    [InlineData("")]
    public void Should_return_empty_for_null_or_empty(string? input)
    {
        Assert.Equal(string.Empty, _sut.Sanitize(input));
    }
}
