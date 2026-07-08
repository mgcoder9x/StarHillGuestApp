namespace Bedrock.Application.Messaging.Dispatch;

/// <summary>
/// Ánh xạ <c>EventType</c> (string ổn định) → CLR type để consumer deserialize payload an toàn (R17.3/AD-006).
/// Host build registry lúc boot từ các assembly <c>*.Contracts</c> đã đăng ký. <c>EventType</c> lạ → dead-letter,
/// KHÔNG crash consumer.
/// </summary>
public interface IIntegrationEventTypeRegistry
{
    /// <summary>Trả CLR type cho <paramref name="eventType"/>, hoặc null nếu chưa đăng ký (→ dead-letter).</summary>
    Type? Resolve(string eventType);
}
