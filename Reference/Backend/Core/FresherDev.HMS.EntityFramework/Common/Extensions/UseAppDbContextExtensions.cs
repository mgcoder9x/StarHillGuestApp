using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FresherDev.HMS.EntityFramework;

public static class UseAppDbContextExtensions
{
    public static void AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            //options.UseSqlServer(configuration.GetSqlServerConnectionString());
            options.UseNpgsql(configuration.GetPostgresConnectionString());
        }, ServiceLifetime.Scoped);
    }
}
