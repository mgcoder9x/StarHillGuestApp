using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Nguồn duy nhất cấu hình Npgsql cho Identity ở runtime, design-time và test. History table được đặt tường
/// minh trong schema Identity; HasDefaultSchema của model không cấu hình migrations history repository.
/// </summary>
public static class IdentityNpgsqlOptionsExtensions
{
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public static DbContextOptionsBuilder UseIdentityNpgsql(
        this DbContextOptionsBuilder options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(
                MigrationsHistoryTableName,
                IdentityDbContext.SchemaName));
    }
}
