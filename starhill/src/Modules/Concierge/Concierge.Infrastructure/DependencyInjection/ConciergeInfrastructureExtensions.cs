using Bedrock.Infrastructure.DependencyInjection;
using Concierge.Contracts;
using Concierge.Domain;
using Concierge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concierge.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Concierge (mirror Housekeeping/Rules/Faq). KEYED persistence (P0-1 fix):
/// <see cref="ConciergeDbContext"/> + repository (Conversation/Message/InternalNote) + Unit of Work theo
/// <see cref="PersistenceKey"/>. KHÔNG map Outbox/Inbox (cascade là event PHÍA GuestAccess — C-GA.5). K-Con.1 =
/// persistence nền; use case (send/reply/read/close + notes + read-model + realtime notifier) đăng ký ở K-Con.2
/// (factory keyed, mirror Housekeeping).
/// </summary>
public static class ConciergeInfrastructureExtensions
{
    /// <summary>Module key persistence của Concierge (= schema <c>concierge</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = ConciergeModule.PersistenceKey;

    public static IServiceCollection AddConciergeInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "concierge"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<ConciergeDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate KEYED theo ConciergeDbContext.
        services.AddBedrockRepository<ConciergeDbContext, Conversation>(PersistenceKey);
        services.AddBedrockRepository<ConciergeDbContext, Message>(PersistenceKey);
        services.AddBedrockRepository<ConciergeDbContext, InternalNote>(PersistenceKey);

        return services;
    }
}
