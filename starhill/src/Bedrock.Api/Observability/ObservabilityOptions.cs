namespace Bedrock.Api.Observability;

/// <summary>
/// Cấu hình observability của base. <see cref="MaskedPathPrefixes"/> do APP khai (F2) — lõi KHÔNG hardcode
/// path nghiệp vụ. Mọi nơi log path (request-logging + exception handler) dùng chung <see cref="PathMasker"/>
/// đọc options này (CP13/F15). Bind từ section "Observability".
/// </summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>
    /// Các prefix path nhạy cảm cần che phần đuôi (thường chứa token). Ví dụ app đăng ký: "/api/guest/resolve/",
    /// "/r/". Get-only collection để config binding populate (tránh CA1819 property-trả-mảng).
    /// </summary>
    public IList<string> MaskedPathPrefixes { get; } = new List<string>();

    /// <summary>Chuỗi thay thế phần nhạy cảm sau prefix. Mặc định "***".</summary>
    public string MaskPlaceholder { get; set; } = "***";
}
