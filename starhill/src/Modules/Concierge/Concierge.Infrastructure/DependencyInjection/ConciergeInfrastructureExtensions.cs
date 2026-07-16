using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using Concierge.Application;
using Concierge.Contracts;
using Concierge.Domain;
using Concierge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;

namespace Concierge.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Concierge (mirror Housekeeping/Rules/Faq). KEYED persistence (P0-1 fix):
/// <see cref="ConciergeDbContext"/> + repository (Conversation/Message/InternalNote) + Unit of Work theo
/// <see cref="PersistenceKey"/>. KHÔNG map Outbox/Inbox (cascade là event PHÍA GuestAccess — C-GA.5). K-Con.2a:
/// read-model + notifier no-op + use case guest (send/read). Staff/notes use case K-Con.2b; SignalR override K-Con.4.
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

        // Read-model (F9). DbContext cụ thể (unkeyed — dùng chung scope).
        services.AddScoped<IConciergeReader, EfConciergeReader>();

        // Realtime notifier NO-OP mặc định (Host OVERRIDE bằng SignalRConciergeNotifier — K-Con.4). KHÔNG keyed.
        services.AddSingleton<IConciergeRealtimeNotifier, NoOpConciergeRealtimeNotifier>();

        // Use case guest (K-Con.2a): factory resolve repo/UoW keyed (mirror Housekeeping). Value-returning IUseCase tự quản SaveChanges.
        services.AddScoped<IUseCase<SendGuestMessageInput, SendGuestMessageResult>>(sp => new SendGuestMessageUseCase(
            sp.GetRequiredService<IResortGuestConfigQuery>(),
            sp.GetRequiredService<IRuleGate>(),
            sp.GetRequiredService<IResortSettingsQuery>(),
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<Message>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IConciergeRealtimeNotifier>()));

        services.AddScoped<IUseCase<GetGuestConversationInput, GetGuestConversationResult>>(sp => new GetGuestConversationUseCase(
            sp.GetRequiredService<IConciergeReader>(),
            sp.GetRequiredKeyedService<IRepository<Message>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        return services;
    }
}
