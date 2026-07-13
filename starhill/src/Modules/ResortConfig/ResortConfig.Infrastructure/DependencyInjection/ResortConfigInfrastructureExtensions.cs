using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using ResortConfig.Domain;
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

        // KEYED persistence (P0-1 fix, design §4.6): DbContext + DB readiness check (schema "resort_config").
        // Module KHÔNG map Outbox/Inbox/RefreshToken (QR-AD-008) và các thành phần dưới inject ResortConfigDbContext
        // CỤ THỂ (không dùng IUnitOfWork/IRepository dùng chung) → chỉ cần persistence keyed để không đụng registry.
        services.AddBedrockPersistence<ResortConfigDbContext>(ResortConfigModule.PersistenceKey, configureDbContext);

        // Resolver i18n: thuật toán thuần, đăng ký thủ công (Contracts không mang marker ISingletonService — QR-DV-002).
        services.AddSingleton<ITranslationResolver, TranslationResolver>();

        // Seeder idempotent (Req 12.4) — scoped (dùng chung scope/DbContext). Host gọi sau migrate (gated dev/compose).
        services.AddScoped<ResortConfigSeeder>();

        // Query đọc settings (Contracts) cho module khác (consumer đầu: Rooms.RenderQrPng đọc GuestWebBaseUrl).
        services.AddScoped<IResortSettingsQuery, EfResortSettingsQuery>();

        // Query kiểm tồn tại resort (Contracts) — Rooms.CreateRoom thẩm định ResortId trước khi tạo phòng (P1(a)).
        services.AddScoped<IResortExistenceQuery, EfResortExistenceQuery>();

        // WRITE-PATH (B-Config.3): admin sửa settings. Repository ResortSettings KEYED theo ResortConfigDbContext
        // (write-path đầu tiên của module cần abstraction ghi — trước đây chỉ read inject DbContext). Use case dùng
        // factory resolve repo/UoW bằng module key (mirror Rooms) → giữ Application ⊥ Infrastructure.
        services.AddBedrockRepository<ResortConfigDbContext, ResortSettings>(ResortConfigModule.PersistenceKey);

        services.AddScoped<ICommandUseCase<UpdateResortSettingsInput>>(sp => new UpdateResortSettingsUseCase(
            sp.GetRequiredKeyedService<IRepository<ResortSettings>>(ResortConfigModule.PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(ResortConfigModule.PersistenceKey)));

        services.AddTransient<IValidator<UpdateResortSettingsInput>, UpdateResortSettingsValidator>();

        return services;
    }
}
