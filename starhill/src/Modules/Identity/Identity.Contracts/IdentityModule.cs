namespace Identity.Contracts;

/// <summary>
/// Hằng định danh module Identity cho KEYED persistence (design §4.6 — mỗi module 1 DbContext + 1 schema).
/// Giá trị = schema <c>identity</c>. Dùng làm module key khi Host/Infrastructure gọi
/// <c>AddBedrockPersistence&lt;IdentityDbContext&gt;(PersistenceKey, ...)</c> và khi use case khai
/// <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của bounded context (chống last-registration-wins P0-1).
/// </summary>
public static class IdentityModule
{
    public const string PersistenceKey = "identity";
}
