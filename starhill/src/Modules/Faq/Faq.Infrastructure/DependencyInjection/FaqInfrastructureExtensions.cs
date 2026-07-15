using Bedrock.Infrastructure.DependencyInjection;
using Faq.Contracts;
using Faq.Domain;
using Faq.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Faq.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Faq (mirror Rules/Rooms/GuestAccess). KEYED persistence (P0-1 fix):
/// <see cref="FaqDbContext"/> + repository (FaqCategory/FaqCategoryTranslation/FaqItem/FaqItemTranslation) + Unit of
/// Work theo <see cref="PersistenceKey"/> → resolve đúng module. Faq KHÔNG map Outbox/Inbox (chưa phát event).
/// E-Faq.1 = persistence nền; use case CRUD/reorder/read đăng ký ở E-Faq.2..4 (factory keyed, mirror Rules).
/// </summary>
public static class FaqInfrastructureExtensions
{
    /// <summary>Module key persistence của Faq (= schema <c>faq</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = FaqModule.PersistenceKey;

    public static IServiceCollection AddFaqInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "faq"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<FaqDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate KEYED theo FaqDbContext (không dùng generic IRepository<> unkeyed global).
        services.AddBedrockRepository<FaqDbContext, FaqCategory>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqCategoryTranslation>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqItem>(PersistenceKey);
        services.AddBedrockRepository<FaqDbContext, FaqItemTranslation>(PersistenceKey);

        return services;
    }
}
