using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using Concierge.Application;
using Concierge.Contracts;
using Concierge.Domain;
using Concierge.Infrastructure.Persistence;
using FluentValidation;
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

        // Query-port stats cho Host Dashboard (QR-AD-002, đọc-đếm). Contracts interface — Id trần.
        services.AddScoped<Concierge.Contracts.IConciergeStatsQuery, EfConciergeStatsQuery>();

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

        // Use case staff (K-Con.2b). Reply value-returning IUseCase (tự SaveChanges, mirror SendGuestMessage);
        // mark-read/close/close-for-visit là ICommandUseCase void (decorator mở transaction theo PersistenceKey).
        services.AddScoped<IUseCase<ReplyConversationInput, ReplyConversationResult>>(sp => new ReplyConversationUseCase(
            sp.GetRequiredService<IResortSettingsQuery>(),
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<Message>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IConciergeRealtimeNotifier>()));

        services.AddScoped<ICommandUseCase<MarkConversationReadInput>>(sp => new MarkConversationReadUseCase(
            sp.GetRequiredService<IConciergeReader>(),
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<Message>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IConciergeRealtimeNotifier>()));

        services.AddScoped<ICommandUseCase<CloseConversationInput>>(sp => new CloseConversationUseCase(
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IConciergeRealtimeNotifier>()));

        services.AddScoped<ICommandUseCase<CloseConversationForVisitInput>>(sp => new CloseConversationForVisitUseCase(
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IConciergeRealtimeNotifier>()));

        // Use case notes (K-Con.2b). Create value-returning IUseCase; update/delete là ICommandUseCase void.
        services.AddScoped<IUseCase<CreateInternalNoteInput, CreateInternalNoteResult>>(sp => new CreateInternalNoteUseCase(
            sp.GetRequiredKeyedService<IRepository<InternalNote>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<Conversation>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<ICommandUseCase<UpdateInternalNoteInput>>(sp => new UpdateInternalNoteUseCase(
            sp.GetRequiredKeyedService<IRepository<InternalNote>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<ICommandUseCase<DeleteInternalNoteInput>>(sp => new DeleteInternalNoteUseCase(
            sp.GetRequiredKeyedService<IRepository<InternalNote>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        // Validators (K-Con.2b).
        services.AddTransient<IValidator<ReplyConversationInput>, ReplyConversationValidator>();
        services.AddTransient<IValidator<CreateInternalNoteInput>, CreateInternalNoteValidator>();
        services.AddTransient<IValidator<UpdateInternalNoteInput>, UpdateInternalNoteValidator>();

        return services;
    }
}
