namespace ResortConfig.Contracts;

/// <summary>
/// Hằng định danh module ResortConfig cho KEYED persistence (design §4.6). Giá trị = schema <c>resort_config</c>.
/// Module KHÔNG dùng IUnitOfWork/IRepository dùng chung (seeder/query inject ResortConfigDbContext cụ thể), nhưng
/// VẪN phải đăng ký persistence KEYED để không đụng <c>PersistenceRegistrationRegistry</c> (2 DbContext unkeyed = ném).
/// </summary>
public static class ResortConfigModule
{
    public const string PersistenceKey = "resort_config";
}
