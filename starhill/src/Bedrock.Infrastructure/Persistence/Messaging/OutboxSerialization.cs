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
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
