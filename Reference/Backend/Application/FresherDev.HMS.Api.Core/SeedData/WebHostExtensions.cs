using FresherDev.HMS.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace FresherDev.HMS.Api.Core.SeedData;

public static class WebHostExtensions
{
    public static void SeedData(this IServiceProvider service, bool isDevelopment)
    {
        using (var scope = service.CreateScope())
        {
            var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (context == null)
            {
                Console.WriteLine("Context is NULL");
                return;
            }

            new AddressSeeder(context, isDevelopment).SeedData();
            new UserSeeder(context, isDevelopment).SeedData();
            new TokenSeeder(context, isDevelopment).SeedData();
        }
    }
}