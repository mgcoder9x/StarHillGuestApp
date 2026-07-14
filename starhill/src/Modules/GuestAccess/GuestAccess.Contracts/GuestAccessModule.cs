namespace GuestAccess.Contracts;

/// <summary>
/// Hằng định danh module GuestAccess cho KEYED persistence (schema <c>guest_access</c>). Dùng khi
/// Host/Infrastructure gọi <c>AddBedrockPersistence&lt;GuestAccessDbContext&gt;(PersistenceKey, ...)</c> và khi
/// use case ghi khai <c>PersistenceKey</c> → resolver chọn ĐÚNG Unit of Work của GuestAccess (chống
/// last-registration-wins P0-1). Nguồn duy nhất ở Contracts.
/// </summary>
public static class GuestAccessModule
{
    public const string PersistenceKey = "guest_access";
}
