using Bedrock.Infrastructure.DependencyInjection;
using Housekeeping.Contracts;
using Housekeeping.Domain;
using Housekeeping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeping.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Housekeeping (mirror Rules/Faq/Rooms). KEYED persistence (P0-1 fix):
/// <see cref="HousekeepingDbContext"/> + repository (HousekeepingTicket/HousekeepingEvent) + Unit of Work theo
/// <see cref="PersistenceKey"/>. KHÔNG map Outbox/Inbox (cascade là event PHÍA GuestAccess — C-GA.5). H-Hk.1 =
/// persistence nền; use case (create/status/complete/read-model) đăng ký ở H-Hk.2 (factory keyed, mirror Faq).
/// </summary>
public static class HousekeepingInfrastructureExtensions
{
    /// <summary>Module key persistence của Housekeeping (= schema <c>housekeeping</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = HousekeepingModule.PersistenceKey;

    public static IServiceCollection AddHousekeepingInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "housekeeping"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<HousekeepingDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate KEYED theo HousekeepingDbContext.
        services.AddBedrockRepository<HousekeepingDbContext, HousekeepingTicket>(PersistenceKey);
        services.AddBedrockRepository<HousekeepingDbContext, HousekeepingEvent>(PersistenceKey);

        return services;
    }
}
