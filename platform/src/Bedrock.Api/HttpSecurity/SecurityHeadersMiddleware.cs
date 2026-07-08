using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Slot #5 pipeline (§3.5): áp security headers phòng thủ cho MỌI response (guest/admin/internal — mọi bề mặt
/// công khai). Set trước khi response bắt đầu. HSTS/HTTPS redirect là slot #4 riêng (task 11). Giá trị cụ thể
/// (CSP...) có thể mở rộng qua options ở task 11; đây là bộ header an toàn tối thiểu, trung lập nghiệp vụ.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}
