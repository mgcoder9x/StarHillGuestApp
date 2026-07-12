using Bedrock.Api.HttpSecurity;
using Xunit;

namespace Bedrock.Api.Tests.HttpSecurity;

/// <summary>
/// A-16 guard LOCAL cho <see cref="HttpSecurityOptions.Validate"/> — fail-fast cấu hình HTTP hardening (F16/F17):
/// proxy/network/rate-limit/CORS sai chặn boot (gọi eager trong `AddBedrockHttpSecurity`). Đặc biệt: CIDR/IP không
/// hợp lệ KHÔNG còn bị bỏ qua âm thầm (điểm review A-16 nêu), và CrossSite bắt buộc origin cụ thể (không wildcard).
/// </summary>
public sealed class HttpSecurityOptionsTests
{
    [Fact]
    public void Default_same_site_options_are_valid()
    {
        HttpSecurityOptions.Validate(new HttpSecurityOptions()); // same-site, không cần CORS origin — không ném.
    }

    [Fact]
    public void Non_positive_forward_limit_fails()
    {
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(new HttpSecurityOptions { ForwardLimit = 0 }));
    }

    [Theory]
    [InlineData(0, 60, 0)]   // PermitLimit <= 0
    [InlineData(100, 0, 0)]  // WindowSeconds <= 0
    [InlineData(100, 60, -1)] // QueueLimit < 0
    public void Invalid_rate_limit_fails(int permit, int window, int queue)
    {
        var options = new HttpSecurityOptions
        {
            RateLimitPermitLimit = permit,
            RateLimitWindowSeconds = window,
            RateLimitQueueLimit = queue,
        };
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(options));
    }

    [Fact]
    public void Empty_cors_policy_name_fails()
    {
        Assert.Throws<ArgumentException>(() => HttpSecurityOptions.Validate(new HttpSecurityOptions { CorsPolicyName = "  " }));
    }

    [Fact]
    public void Invalid_known_proxy_ip_fails() // A-16: IP proxy sai KHÔNG bị bỏ qua âm thầm
    {
        var options = new HttpSecurityOptions();
        options.KnownProxies.Add("not-an-ip");
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(options));
    }

    [Fact]
    public void Invalid_known_network_cidr_fails()
    {
        var options = new HttpSecurityOptions();
        options.KnownNetworks.Add("10.0.0.0/999");
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(options));
    }

    [Fact]
    public void Cross_site_without_origins_fails()
    {
        var options = new HttpSecurityOptions { CookieSameSiteMode = CookieSameSiteMode.CrossSite };
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(options));
    }

    [Fact]
    public void Cross_site_wildcard_origin_fails() // credentials + "*" là cấu hình nguy hiểm → chặn
    {
        var options = new HttpSecurityOptions { CookieSameSiteMode = CookieSameSiteMode.CrossSite };
        options.CorsAllowedOrigins.Add("*");
        Assert.Throws<InvalidOperationException>(() => HttpSecurityOptions.Validate(options));
    }

    [Fact]
    public void Cross_site_with_explicit_origin_is_valid()
    {
        var options = new HttpSecurityOptions { CookieSameSiteMode = CookieSameSiteMode.CrossSite };
        options.CorsAllowedOrigins.Add("https://app.example.com");
        HttpSecurityOptions.Validate(options); // origin tuyệt đối, scheme https, không path → hợp lệ.
    }
}
