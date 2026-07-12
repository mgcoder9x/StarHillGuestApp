namespace Bedrock.Api.Observability;

/// <summary>
/// Masker path dùng chung (CP13/F15): che phần đuôi của path khi prefix nằm trong
/// <see cref="ObservabilityOptions.MaskedPathPrefixes"/>. Prefix lấy từ OPTIONS (app khai) — KHÔNG hardcode
/// path nghiệp vụ trong lõi (gỡ tận gốc F2). Dùng ở CẢ request-logging lẫn exception handler để token không
/// bao giờ lọt vào log (kể cả request lỗi 500 — chính lỗ hổng F15).
/// </summary>
public sealed class PathMasker
{
    private readonly string[] _prefixes;
    private readonly string _placeholder;

    public PathMasker(ObservabilityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _prefixes = [.. options.MaskedPathPrefixes];
        _placeholder = options.MaskPlaceholder;
    }

    /// <summary>
    /// Trả path đã che nếu khớp một prefix nhạy cảm (mọi ký tự sau prefix → placeholder), ngược lại trả nguyên.
    /// Null/rỗng → trả chuỗi rỗng chuẩn hóa.
    /// </summary>
    public string Mask(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        foreach (var prefix in _prefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix + _placeholder;
            }
        }

        return path;
    }
}
