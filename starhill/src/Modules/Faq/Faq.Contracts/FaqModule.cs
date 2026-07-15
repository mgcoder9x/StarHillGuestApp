namespace Faq.Contracts;

/// <summary>
/// Hằng định danh module Faq cho KEYED persistence (schema <c>faq</c>). Dùng khi Host/Infrastructure gọi
/// <c>AddBedrockPersistence&lt;FaqDbContext&gt;(PersistenceKey, ...)</c> và khi use case ghi khai
/// <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của Faq (chống last-registration-wins P0-1).
/// Nguồn duy nhất ở Contracts (mirror RulesModule/GuestAccessModule).
/// </summary>
public static class FaqModule
{
    public const string PersistenceKey = "faq";
}
