using ResortQr.Application.Identity;
using ResortQr.Application.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Wiring persistence CỤ THỂ của app: đăng ký <see cref="AppDbContext"/> (Npgsql + snake_case) rồi nối
/// tầng nền qua <c>AddResortQrPersistence&lt;AppDbContext&gt;()</c> (UnitOfWork/RefreshTokenStore/health).
/// </summary>
public static class AppPersistenceExtensions
{
    public static IServiceCollection AddResortQrDatabase(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddResortQrPersistence<AppDbContext>();

        // Store/query coupled AppDbContext → wire TƯỜNG MINH cùng chỗ (loại khỏi Scrutor auto-scan, DEC-049).
        services.TryAddScoped<IUserAuthStore, AppUserAuthStore>();
        services.TryAddScoped<IRoomQueries, EfRoomQueries>();

        return services;
    }
}
