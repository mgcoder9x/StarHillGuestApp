using System.Text.RegularExpressions;
using Bedrock.Application.Messaging.Dispatch;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// P1-07 — Định dạng lỗi publish để lưu vào cột chẩn đoán <see cref="OutboxMessage.LastError"/> theo hướng
/// AN-TOÀN-DỮ-LIỆU: (1) giữ CLASSIFICATION ổn định = tên kiểu exception (máy-đọc, không nhạy cảm); (2) REDACT thật
/// các mẫu nhạy cảm phổ biến trong <c>Exception.Message</c> (credential trong URI, cặp key nhạy cảm, Bearer token)
/// TRƯỚC khi lưu — KHÔNG chỉ cắt độ dài; (3) BOUND tối đa <see cref="OutboxMessage.MaxLastErrorLength"/> ký tự.
/// <para>
/// Hàm THUẦN (không I/O) để unit-test đầy đủ luật redaction. Tên cũ <c>SanitizeError</c> gây hiểu nhầm "đã làm
/// sạch" trong khi chỉ truncate (re-audit P1-07) → tách + đổi tên <see cref="Redact"/> phản ánh đúng hành vi.
/// LƯU Ý: redaction dựa mẫu là BEST-EFFORT (giảm rủi ro rò), không thay thế việc chỉ log detail đầy đủ vào sink
/// đã kiểm soát access. Cột outbox chỉ giữ bản đã redact + bound.
/// </para>
/// </summary>
public static partial class OutboxErrorFormatter
{
    /// <summary>Định dạng + redact + bound lỗi thành chuỗi lưu <see cref="OutboxMessage.LastError"/>.</summary>
    public static string Redact(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = exception.Message ?? string.Empty;
        message = UriCredentialsRegex().Replace(message, "$1***:***@"); // scheme://user:pass@ → scheme://***:***@
        message = BearerTokenRegex().Replace(message, "Bearer ***");     // Bearer <token> → Bearer ***
        message = SensitiveKvRegex().Replace(message, "$1=***");         // password=.. / token:.. / api_key=.. → key=***

        var formatted = $"{exception.GetType().Name}: {message}";
        return formatted.Length <= OutboxMessage.MaxLastErrorLength
            ? formatted
            : formatted[..OutboxMessage.MaxLastErrorLength];
    }

    // Credential trong URI: giữ scheme (group 1), xoá user (group 2) + password (group 3).
    [GeneratedRegex(@"([a-zA-Z][a-zA-Z0-9+.\-]*://)([^:/?#\s]+):([^@/?#\s]+)@",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex UriCredentialsRegex();

    // Bearer token trong message (Authorization header dump, v.v.).
    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    // Cặp key nhạy cảm dạng key=value hoặc key: value; giữ tên key (group 1), xoá value tới ranh giới ; , & hoặc space.
    [GeneratedRegex(@"\b(password|pwd|passwd|token|secret|api[_-]?key|access[_-]?key|authorization|auth)\b\s*[=:]\s*[^\s;,&]+",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveKvRegex();
}
