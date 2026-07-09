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
        // EventType là mã ổn định (thường literal) → đọc qua instance CHƯA init (không chạy constructor,
        // không cần biết tham số). Nếu getter phụ thuộc field instance → sẽ lộ ra là lỗi thiết kế cần sửa.
        var probe = (IntegrationEvent)RuntimeHelpers.GetUninitializedObject(type);
        var eventType = probe.EventType;
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException(
                $"'{type.FullName}.EventType' rỗng — integration event phải trả mã ổn định khác rỗng.");
        }

        return eventType;
    }
}
