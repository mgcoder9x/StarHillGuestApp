using Bedrock.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using ResortConfig.Infrastructure.Persistence;

namespace ResortConfig.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure của composition module ResortConfig (mirror IdentityInfrastructureExtensions/DV-013).
/// Đăng ký: <see cref="ResortConfigDbContext"/> + persistence nền (repo/UoW/DB health) qua
/// <c>AddBedrockPersistence</c>, và resolver i18n singleton (đăng ký THỦ CÔNG — QR-DV-002). Host gọi method này
/// (+ <c>AddResortConfigApi</c> khi có Api slice B.3) rồi <c>AddBedrockCore</c>.
/// </summary>
public static class ResortConfigInfrastructureExtensions
{
    public static IServiceCollection AddResortConfigInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + repo/UoW/outbox-writer/refresh-store + DB readiness check (schema "resort_config").
        services.AddBedrockPersistence<ResortConfigDbContext>(configureDbContext);

        // Resolver i18n: thuật toán thuần, đăng ký thủ công (Contracts không mang marker ISingletonService — QR-DV-002).
        services.AddSingleton<ITranslationResolver, TranslationResolver>();

        // Seeder idempotent (Req 12.4) — scoped (dùng chung scope/DbContext). Host gọi sau migrate (gated dev/compose).
        services.AddScoped<ResortConfigSeeder>();

        // Query đọc settings (Contracts) cho module khác (consumer đầu: Rooms.RenderQrPng đọc GuestWebBaseUrl).
        services.AddScoped<IResortSettingsQuery, EfResortSettingsQuery>();

        return services;
    }
}
