namespace Housekeeping.Contracts;

/// <summary>
/// Hằng định danh module Housekeeping cho KEYED persistence (schema <c>housekeeping</c>). Dùng khi Host/Infrastructure
/// gọi <c>AddBedrockPersistence&lt;HousekeepingDbContext&gt;(PersistenceKey, ...)</c> và khi use case ghi khai
/// <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của Housekeeping (chống last-registration-wins P0-1).
/// Nguồn duy nhất ở Contracts (mirror RulesModule/FaqModule/GuestAccessModule).
/// </summary>
public static class HousekeepingModule
{
    public const string PersistenceKey = "housekeeping";
}
