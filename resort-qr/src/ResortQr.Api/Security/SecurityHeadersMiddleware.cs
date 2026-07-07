using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ResortQr.Api.Security;

/// <summary>
/// Gắn security header cho MỌI response (Req 20.4): Content-Security-Policy + X-Content-Type-Options: nosniff.
/// Áp cho toàn bộ bề mặt (guest/admin/hub) vì đặt sớm trong pipeline (Req 20.7). Đặt TRƯỚC khi vào next
/// để header có mặt kể cả response lỗi/401.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityOptions options)
    {
        _next = next;
        _options = options;
    }

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;
        headers[HeaderNames.ContentSecurityPolicy] = _options.ContentSecurityPolicy;
        headers[HeaderNames.XContentTypeOptions] = "nosniff";

        return _next(context);
    }
}
