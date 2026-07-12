using Bedrock.Api.HttpSecurity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bedrock.Api.Tests.HttpSecurity;

/// <summary>Task 11 — cấu hình HTTP hardening: ForwardedHeaders bind từ options + cookie flags theo mode (F16/F17).</summary>
public sealed class HttpSecurityConfigTests
{
    [Fact]
    public void ForwardedHeaders_are_configured_from_options()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HttpSecurity:KnownProxies:0"] = "10.0.0.1",
                ["HttpSecurity:KnownNetworks:0"] = "10.0.0.0/8",
                ["HttpSecurity:ForwardLimit"] = "2",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddBedrockHttpSecurity(configuration);
        using var provider = services.BuildServiceProvider();

        var forwarded = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.True(forwarded.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(forwarded.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
        Assert.Equal(2, forwarded.ForwardLimit);
        Assert.Contains(forwarded.KnownProxies, ip => ip.ToString() == "10.0.0.1");
        Assert.Single(forwarded.KnownIPNetworks);
    }

    [Theory]
    [InlineData(CookieSameSiteMode.SameSite, SameSiteMode.Lax, false)]
    [InlineData(CookieSameSiteMode.CrossSite, SameSiteMode.None, true)]
    public void Cookie_flags_match_mode(CookieSameSiteMode mode, SameSiteMode expectedSameSite, bool expectedRequiresCsrf)
    {
        var cookie = CookieSecurityDefaults.CreateCookieOptions(mode);

        Assert.Equal(expectedSameSite, cookie.SameSite);
        Assert.True(cookie.Secure);   // không gửi cookie qua HTTP trần.
        Assert.True(cookie.HttpOnly); // chặn JS đọc cookie.
        Assert.Equal(expectedRequiresCsrf, CookieSecurityDefaults.RequiresCsrf(mode));
    }

    // A-15: antiforgery (CSRF) đăng ký AN TOÀN theo mode — header X-CSRF-TOKEN + cookie HttpOnly/Secure + SameSite khớp mode.
    [Theory]
    [InlineData(CookieSameSiteMode.SameSite, SameSiteMode.Lax)]
    [InlineData(CookieSameSiteMode.CrossSite, SameSiteMode.None)]
    public void Antiforgery_is_configured_secure_by_mode(CookieSameSiteMode mode, SameSiteMode expectedSameSite)
    {
        var settings = new Dictionary<string, string?>
        {
            ["HttpSecurity:CookieSameSiteMode"] = mode.ToString(),
        };
        if (mode == CookieSameSiteMode.CrossSite)
        {
            settings["HttpSecurity:CorsAllowedOrigins:0"] = "https://app.example.com"; // CrossSite bắt buộc origin cụ thể.
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddBedrockHttpSecurity(configuration);
        using var provider = services.BuildServiceProvider();

        var antiforgery = provider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        Assert.Equal("X-CSRF-TOKEN", antiforgery.HeaderName);
        Assert.Equal(expectedSameSite, antiforgery.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, antiforgery.Cookie.SecurePolicy); // CSRF cookie chỉ qua HTTPS.
        Assert.True(antiforgery.Cookie.HttpOnly);
    }
}
