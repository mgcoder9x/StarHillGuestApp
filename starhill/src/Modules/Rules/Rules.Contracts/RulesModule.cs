namespace Rules.Contracts;

/// <summary>
/// Hằng định danh module Rules cho KEYED persistence (schema <c>rules</c>). Dùng khi Host/Infrastructure gọi
/// <c>AddBedrockPersistence&lt;RulesDbContext&gt;(PersistenceKey, ...)</c> và khi use case ghi khai
/// <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của Rules (chống last-registration-wins P0-1).
/// Nguồn duy nhất ở Contracts.
/// </summary>
public static class RulesModule
{
    public const string PersistenceKey = "rules";
}
