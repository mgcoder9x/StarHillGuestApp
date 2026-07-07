using System.Text.Json.Serialization;
using FresherDev.HMS.Api.Core.SeedData;
using FresherDev.HMS.Common;
using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Force load assemblies
        ForceLoadAssemblies();

        // Add services to the container.
        builder.Services.AddApplicationDbContext(builder.Configuration);
        // TODO: Remove this line
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddAutoDependency();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Add services to the container.
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(Program).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Services.SeedData(app.Environment.IsDevelopment());

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void ForceLoadAssemblies()
    {
        // Core
        FresherDev.HMS.Auth.ForceLoadAssembly.Instance.ForceLoad();
        FresherDev.HMS.Infrastructure.ForceLoadAssembly.Instance.ForceLoad();
        // FresherDev.HMS.Common
        // FresherDev.HMS.EntityFramework

        // Domain
        FresherDev.HMS.Core.ForceLoadAssembly.Instance.ForceLoad();
        FresherDev.HMS.Core.Shared.ForceLoadAssembly.Instance.ForceLoad();
        FresherDev.HMS.Domain.ForceLoadAssembly.Instance.ForceLoad();

        // Application
        FresherDev.HMS.Api.Core.ForceLoadAssembly.Instance.ForceLoad();
        FresherDev.HMS.Api.Core.Shared.ForceLoadAssembly.Instance.ForceLoad();
        // FresherDev.HMS.Api
    }
}
