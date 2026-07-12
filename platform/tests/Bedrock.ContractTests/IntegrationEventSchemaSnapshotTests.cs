using System.Reflection;
using System.Runtime.CompilerServices;
using Bedrock.Messaging.Contracts;
using Identity.Contracts.Events;

namespace Bedrock.ContractTests;

/// <summary>
/// Task 17 (F32/§9.1, R32.4) — SNAPSHOT schema của mọi integration event public. Bản chất: chống thay đổi
/// BREAKING VÔ Ý. Mọi khác biệt so với <see cref="ExpectedSchema"/> làm FAIL BUILD → buộc quyết định CÓ Ý THỨC:
/// thêm field optional (backward-compat) thì cập nhật snapshot; thay đổi breaking (đổi/xoá field, đổi kiểu)
/// thì phải tạo <c>EventType</c> mới (v2) chạy song song (R22.3) chứ KHÔNG sửa event cũ.
/// <para>
/// Descriptor mỗi event = <c>{EventType} v{SchemaVersion} {{ Prop:Type, ... }}</c> (prop sắp theo tên,
/// loại <c>EventType</c>/<c>SchemaVersion</c> vì là metadata đã tách riêng). Đọc <c>EventType</c>/<c>SchemaVersion</c>
/// qua instance CHƯA khởi tạo (chúng trả literal, không đụng field) — không cần dựng ctor có tham số.
/// </para>
/// </summary>
public sealed class IntegrationEventSchemaSnapshotTests
{
    // SNAPSHOT ĐÃ DUYỆT — cập nhật CÓ Ý THỨC khi thêm/đổi event (kèm review versioning F32).
    private const string ExpectedSchema =
        "identity.user_token_refreshed v1 { Id:Guid, OccurredAt:DateTimeOffset, UserId:Guid }";

    [Fact]
    public void Integration_event_schema_matches_snapshot()
    {
        Assembly[] contractAssemblies =
        [
            typeof(IntegrationEvent).Assembly,                    // Bedrock.Messaging.Contracts (chỉ base abstract)
            typeof(UserTokenRefreshedIntegrationEvent).Assembly,  // Identity.Contracts
        ];

        var actual = string.Join(
            "\n",
            contractAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IntegrationEvent).IsAssignableFrom(t))
                .Select(Describe)
                .OrderBy(s => s, StringComparer.Ordinal));

        Assert.Equal(ExpectedSchema, actual);
    }

    private static string Describe(Type eventType)
    {
        // EventType/SchemaVersion trả literal → an toàn đọc trên instance chưa khởi tạo (không đụng field ctor).
        var instance = (IntegrationEvent)RuntimeHelpers.GetUninitializedObject(eventType);

        var properties = eventType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is not (nameof(IntegrationEvent.EventType) or nameof(IntegrationEvent.SchemaVersion)))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}:{ContractSchema.DescribeProperty(p)}"); // A-22: canonical (generic/array/nullable).

        return $"{instance.EventType} v{instance.SchemaVersion} {{ {string.Join(", ", properties)} }}";
    }
}
