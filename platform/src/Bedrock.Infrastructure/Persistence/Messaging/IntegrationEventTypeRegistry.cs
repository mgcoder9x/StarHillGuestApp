using System.Reflection;
using System.Runtime.CompilerServices;
using Bedrock.Application.Messaging.Dispatch;
using Bedrock.Messaging.Contracts;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Impl <see cref="IIntegrationEventTypeRegistry"/> (R17.3/AD-006). Build lúc boot: quét các assembly
/// <c>*.Contracts</c>, ánh xạ <c>EventType</c> (mã ổn định) → CLR type để consumer deserialize an toàn.
/// <c>EventType</c> lạ → <see cref="Resolve"/> trả <c>null</c> ⇒ consumer dead-letter, KHÔNG crash.
/// </summary>
public sealed class IntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly Dictionary<string, Type> _byEventType;

    public IntegrationEventTypeRegistry(IEnumerable<Assembly> contractsAssemblies)
    {
        ArgumentNullException.ThrowIfNull(contractsAssemblies);

        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assembly in contractsAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(IntegrationEvent).IsAssignableFrom(type))
                {
                    continue;
                }

                var eventType = ReadEventType(type);
                if (map.TryGetValue(eventType, out var existing) && existing != type)
                {
                    throw new InvalidOperationException(
                        $"Trùng EventType '{eventType}' giữa '{existing.FullName}' và '{type.FullName}'. "
                        + "EventType phải DUY NHẤT toàn hệ (hợp đồng routing/deserialize).");
                }

                map[eventType] = type;
            }
        }

        _byEventType = map;
    }

    public Type? Resolve(string eventType) =>
        _byEventType.TryGetValue(eventType, out var type) ? type : null;

    private static string ReadEventType(Type type)
    {
        // EventType là mã ổn định (biểu thức HẰNG/ĐỘC-LẬP-FIELD) → đọc qua instance CHƯA init (không chạy constructor).
        // Nếu getter phụ thuộc field do ctor gán → trên instance chưa init sẽ trả null/ném → đây là LỖI THIẾT KẾ event:
        // bọc để báo lỗi RÕ (nêu type + hợp đồng) thay vì NRE mơ hồ lúc boot (A-21). Guard build-time:
        // Bedrock.ContractTests/IntegrationEventMetadataContractTests enforce hợp đồng này trước cả khi boot.
        string? eventType;
        try
        {
            var probe = (IntegrationEvent)RuntimeHelpers.GetUninitializedObject(type);
            eventType = probe.EventType;
        }
#pragma warning disable CA1031 // Bọc RỘNG có chủ đích: bất kỳ lỗi đọc getter (NRE do field-dependent...) → quy về một lỗi cấu hình rõ ràng.
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Không đọc được '{type.FullName}.EventType' trên instance CHƯA khởi tạo (registry đọc metadata lúc "
                + "boot mà KHÔNG chạy constructor). EventType phải là biểu thức HẰNG/ĐỘC-LẬP-FIELD "
                + "(vd: => \"identity.user_token_refreshed\"), KHÔNG phụ thuộc field do constructor gán.", ex);
        }
#pragma warning restore CA1031

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException(
                $"'{type.FullName}.EventType' rỗng/null trên instance chưa khởi tạo — phải là biểu thức hằng khác "
                + "rỗng (độc-lập-field), là mã ổn định cho routing/deserialize.");
        }

        return eventType;
    }
}
