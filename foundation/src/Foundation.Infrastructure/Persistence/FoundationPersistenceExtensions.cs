using Foundation.Application.Abstractions.Persistence;
using Foundation.Application.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Foundation.Infrastructure.Persistence;

/// <summary>
/// Wire tường minh tầng persistence NỀN của Foundation sau khi app đã <c>AddDbContext&lt;TContext&gt;</c>.
/// Đăng ký: alias <see cref="FoundationDbContext"/> → <typeparamref name="TContext"/> của app,
/// <see cref="IUnitOfWork"/> → <see cref="EfUnitOfWork"/>, <see cref="IRefreshTokenStore"/> → <see cref="EfRefreshTokenStore"/>,
/// và readiness health check DB (tag "ready" — Req 14.6/14.7).
/// Gọi TƯỜNG MINH (không auto-scan) để phụ thuộc DbContext rõ ràng + tránh đăng ký trùng.
/// </summary>
public static class FoundationPersistenceExtensions
{
    /// <param name="services">Service collection (đã <c>AddDbContext&lt;TContext&gt;</c> trước hoặc sau đều được).</param>
    /// <typeparam name="TContext">DbContext cụ thể của app, dẫn xuất <see cref="FoundationDbContext"/>.</typeparam>
    public static IServiceCollection AddFoundationPersistence<TContext>(this IServiceCollection services)
        where TContext : FoundationDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Repository/UoW/Store nền dùng FoundationDbContext (base) → trỏ về DbContext cụ thể của app.
        services.TryAddScoped<FoundationDbContext>(sp => sp.GetRequiredService<TContext>());

        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();
        services.TryAddScoped<IRefreshTokenStore, EfRefreshTokenStore>();

        // Readiness: kiểm kết nối DB (CanConnect) — KHÔNG lộ connection string (Req 14.7). Tag "ready"
        // để endpoint /health/ready lọc riêng khỏi /health/live (Req 14.6).
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>("database", HealthStatus.Unhealthy, tags: ["ready"]);

        return services;
    }
}
