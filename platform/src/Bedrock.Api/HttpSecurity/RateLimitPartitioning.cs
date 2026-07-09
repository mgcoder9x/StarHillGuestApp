using Microsoft.AspNetCore.Http;

namespace Bedrock.Api.HttpSecurity;

/// <summary>
/// Khóa partition rate-limit = IP client THẬT (<see cref="ConnectionInfo.RemoteIpAddress"/> đã được
/// <c>UseForwardedHeaders</c> (#1) resolve từ X-Forwarded-For). Tách ra để rõ ý + test được (F16 — chặn abuse
/// theo IP thật, không theo IP proxy).
/// </summary>
internal static class RateLimitPartitioning
{
    public const string UnknownClient = "unknown";

    public static string ResolveClientKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;
    }
}
