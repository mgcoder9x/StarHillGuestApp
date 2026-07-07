using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace FresherDev.HMS.EntityFramework;

public class ApplicationContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var configuration = GetConfiguration();
        var connectionString = configuration.GetPostgresConnectionString();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(connectionString);
        Console.ResetColor();

        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private IConfiguration GetConfiguration()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var rootFolder = Path.Combine(currentDirectory, "..", "..", "Application", "FresherDev.HMS.Api");

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(rootFolder)
            .AddJsonFile("appsettings.json", false, true);

        var configuration = builder.Build();

        return configuration;
    }
}

// dotnet ef database update --project ./Core/FresherDev.HMS.EntityFramework/FresherDev.HMS.EntityFramework.csproj
// dotnet ef migrations add InitDb --project ./Core/FresherDev.HMS.EntityFramework/FresherDev.HMS.EntityFramework.csproj