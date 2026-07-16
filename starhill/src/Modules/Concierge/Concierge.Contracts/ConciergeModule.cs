namespace Concierge.Contracts;

/// <summary>
/// Hằng định danh module Concierge cho KEYED persistence (schema <c>concierge</c>). Dùng khi Host/Infrastructure gọi
/// <c>AddBedrockPersistence&lt;ConciergeDbContext&gt;(PersistenceKey, ...)</c> và khi use case ghi khai
/// <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của Concierge (chống last-registration-wins P0-1). Nguồn
/// duy nhất ở Contracts (mirror HousekeepingModule/RulesModule/FaqModule). Tên "Concierge" (QR-AD-004) tránh đụng
/// <c>Bedrock.Messaging.Contracts</c> (bus sự kiện hạ tầng base) — đây là nghiệp vụ nhắn tin khách↔lễ tân.
/// </summary>
public static class ConciergeModule
{
    public const string PersistenceKey = "concierge";
}
