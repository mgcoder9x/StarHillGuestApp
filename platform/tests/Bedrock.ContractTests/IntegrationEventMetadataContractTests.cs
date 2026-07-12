using System.Reflection;
using System.Runtime.CompilerServices;
using Bedrock.Messaging.Contracts;
using Identity.Contracts.Events;

namespace Bedrock.ContractTests;

/// <summary>
/// A-21 — ENFORCE (build-gate) hợp đồng METADATA mà <c>IntegrationEventTypeRegistry</c> và
/// <c>IntegrationEventSchemaSnapshotTests</c> DỰA VÀO: <c>EventType</c>/<c>SchemaVersion</c> phải đọc được trên
/// instance CHƯA khởi tạo (registry đọc lúc boot mà KHÔNG chạy constructor). Trước đây hợp đồng này chỉ là quy
/// ước (compiler không ép); một event có getter PHỤ THUỘC FIELD sẽ ném/null lúc boot — lỗi khó hiểu. Test này
/// biến quy ước thành lỗi BUILD rõ ràng (KEYSTONE anti-drift).
/// <para>
/// <b>Vì sao KHÔNG key theo (EventType, SchemaVersion):</b> mô hình versioning của design (R22.2/R22.3, §9.1) là
/// "thêm field ⇒ optional (backward-compat) cùng EventType; breaking ⇒ EventType MỚI (v2) chạy song song +
/// tolerant reader". Do đó mọi SchemaVersion của MỘT EventType luôn tương thích đọc được → key theo EventType là
/// ĐÚNG, version-rejection là KHÔNG cần (xem AD-083). SchemaVersion là metadata cho snapshot/quan sát, không phải khoá.
/// </para>
/// </summary>
public sealed class IntegrationEventMetadataContractTests
{
    // Cùng tập assembly với IntegrationEventSchemaSnapshotTests (auto-discover toàn solution = A-11, hoãn).
    private static readonly Assembly[] ContractAssemblies =
    [
        typeof(IntegrationEvent).Assembly,                   // Bedrock.Messaging.Contracts (chỉ base abstract)
        typeof(UserTokenRefreshedIntegrationEvent).Assembly, // Identity.Contracts
    ];

    private static IReadOnlyList<Type> ConcreteEventTypes() =>
        [.. ContractAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IntegrationEvent).IsAssignableFrom(t))];

    [Fact]
    public void Every_integration_event_exposes_field_independent_non_empty_metadata()
    {
        var types = ConcreteEventTypes();
        Assert.NotEmpty(types); // sanity: phải quét được ít nhất một event (tránh test vacuous).

        foreach (var type in types)
        {
            var probe = (IntegrationEvent)RuntimeHelpers.GetUninitializedObject(type);
            var eventType = ReadOrFail(type, () => probe.EventType, nameof(IntegrationEvent.EventType));
            var schemaVersion = ReadOrFail(type, () => probe.SchemaVersion, nameof(IntegrationEvent.SchemaVersion));

            Assert.False(
                string.IsNullOrWhiteSpace(eventType),
                $"'{type.FullName}.EventType' phải khác rỗng (mã ổn định độc-lập-field).");
            Assert.True(
                schemaVersion >= 1,
                $"'{type.FullName}.SchemaVersion' phải >= 1 (hiện {schemaVersion}).");
        }
    }

    [Fact]
    public void Event_types_are_unique_across_contract_assemblies()
    {
        var duplicates = ConcreteEventTypes()
            .Select(t => ((IntegrationEvent)RuntimeHelpers.GetUninitializedObject(t)).EventType)
            .GroupBy(code => code, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        Assert.Empty(duplicates); // EventType trùng phá routing/deserialize (registry cũng ném — đây là guard sớm hơn).
    }

    private static T ReadOrFail<T>(Type type, Func<T> read, string member)
    {
        try
        {
            return read();
        }
#pragma warning disable CA1031 // Bọc RỘNG có chủ đích: bất kỳ lỗi getter (NRE do field-dependent...) = vi phạm hợp đồng cần báo rõ.
        catch (Exception ex)
        {
            Assert.Fail(
                $"'{type.FullName}.{member}' PHỤ THUỘC FIELD (ném khi đọc trên instance chưa khởi tạo): {ex.Message}. "
                + "Metadata phải là biểu thức HẰNG/ĐỘC-LẬP-FIELD (registry đọc lúc boot, không chạy constructor).");
            return default!; // unreachable — Assert.Fail luôn ném.
        }
#pragma warning restore CA1031
    }
}
