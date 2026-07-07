using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// Wire tường minh tầng persistence NỀN của ResortQr sau khi app đã <c>AddDbContext&lt;TContext&gt;</c>.
/// Đăng ký: alias <see cref="ResortQrDbContext"/> → <typeparamref name="TContext"/> của app,
/// <see cref="IUnitOfWork"/> → <see cref="EfUnitOfWork"/>, <see cref="IRefreshTokenStore"/> → <see cref="EfRefreshTokenStore"/>,
/// và readiness health check DB (tag "ready" — Req 14.6/14.7).
/// Gọi TƯỜNG MINH (không auto-scan) để phụ thuộc DbContext rõ ràng + tránh đăng ký trùng.
/// </summary>
public static class ResortQrPersistenceExtensions
{
    /// <param name="services">Service collection (đã <c>AddDbContext&lt;TContext&gt;</c> trước hoặc sau đều được).</param>
    /// <typeparam name="TContext">DbContext cụ thể của app, dẫn xuất <see cref="ResortQrDbContext"/>.</typeparam>
    public static IServiceCollection AddResortQrPersistence<TContext>(this IServiceCollection services)
        where TContext : ResortQrDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Repository/UoW/Store nền dùng ResortQrDbContext (base) → trỏ về DbContext cụ thể của app.
        services.TryAddScoped<ResortQrDbContext>(sp => sp.GetRequiredService<TContext>());

        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();
        services.TryAddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

        // Readiness: kiểm kết nối DB (CanConnect) — KHÔNG lộ connection string (Req 14.7). Tag "ready"
        // để endpoint /health/ready lọc riêng khỏi /health/live (Req 14.6).
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>("database", HealthStatus.Unhealthy, tags: ["ready"]);

        return services;
    }
}
