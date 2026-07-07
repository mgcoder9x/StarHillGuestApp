using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FresherDev.HMS.EntityFramework;

public static class IConfigurationExtensions
{
    public static string? GetPostgresConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString("PostgresConnection");
    }

    public static string? GetSqlServerConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString("DefaultConnection");
    }
}