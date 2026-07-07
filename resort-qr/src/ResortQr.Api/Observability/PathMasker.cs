namespace ResortQr.Api.Observability;

/// <summary>
/// Che segment token trong request path trước khi ghi log (Req 14.2): tránh lộ PublicToken qua log.
/// `/r/{token}` → `/r/***`; `/api/guest/resolve/{token}` → `/api/guest/resolve/***`. Path khác giữ nguyên.
/// Hàm thuần (không phụ thuộc ASP.NET) → test được.
/// </summary>
public static class PathMasker
{
    private const string ResolvePrefix = "/api/guest/resolve/";
    private const string ShortPrefix = "/r/";

    public static string Mask(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        if (path.StartsWith(ResolvePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ResolvePrefix + "***";
        }

        if (path.StartsWith(ShortPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return ShortPrefix + "***";
        }

        return path;
    }
}
