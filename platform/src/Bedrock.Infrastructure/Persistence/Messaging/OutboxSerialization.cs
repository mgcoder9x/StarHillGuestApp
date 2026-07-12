using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Hợp đồng serialization Outbox CỐ ĐỊNH (AD-015/design §5.2): camelCase + bỏ null. Dùng chung bởi
/// <c>EfOutboxWriter</c> (serialize khi enqueue) và consumer (deserialize) → producer/consumer KHÔNG lệch
/// định dạng. Đổi options = thay đổi có ảnh hưởng version event (gắn versioning F32) — không sửa tuỳ tiện.
/// </summary>
internal static class OutboxSerialization
{
    /// <summary>
    /// Content-type CỐ ĐỊNH của payload integration-event (khớp hợp đồng JSON AD-015/§5.2). Producer (adapter)
    /// gắn header/property này; consumer (dispatcher) validate khớp TRƯỚC khi deserialize (A-10) → message
    /// content-type lạ đi quarantine thay vì cố deserialize rác.
    /// </summary>
    public const string ContentType = "application/json";

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// True nếu <paramref name="contentType"/> là JSON (media type == <see cref="ContentType"/>, bỏ tham số
    /// <c>; charset=...</c>, không phân biệt hoa/thường). Null/rỗng → false (envelope thiếu content-type = không
    /// hợp lệ theo hợp đồng cố định — A-10).
    /// </summary>
    public static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var semicolon = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = (semicolon >= 0 ? contentType[..semicolon] : contentType).Trim();
        return mediaType.Equals(ContentType, StringComparison.OrdinalIgnoreCase);
    }
}
