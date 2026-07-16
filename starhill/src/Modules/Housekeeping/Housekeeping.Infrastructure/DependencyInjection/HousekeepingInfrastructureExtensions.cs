using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Housekeeping.Application;
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

        // Read-model (F9 — GetCurrentTicketByRoom + ListOpenTicketIdsByVisit). DbContext cụ thể (unkeyed).
        services.AddScoped<IHousekeepingReader, EfHousekeepingReader>();

        // Use case (H-Hk.2): factory resolve repo/UoW keyed (mirror Faq/Rules). Value-returning IUseCase tự quản một SaveChanges.
        services.AddScoped<IUseCase<RequestHousekeepingInput, RequestHousekeepingResult>>(sp => new RequestHousekeepingUseCase(
            sp.GetRequiredService<ResortConfig.Contracts.Queries.IResortGuestConfigQuery>(),
            sp.GetRequiredService<Rules.Contracts.IRuleGate>(),
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<GetRoomHousekeepingStatusInput, GetRoomHousekeepingStatusResult>>(sp =>
            new GetRoomHousekeepingStatusUseCase(sp.GetRequiredService<IHousekeepingReader>()));

        services.AddScoped<IUseCase<SetHousekeepingStatusInput, HousekeepingTicketResult>>(sp => new SetHousekeepingStatusUseCase(
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<CompleteHousekeepingByRoomInput, HousekeepingTicketResult>>(sp => new CompleteHousekeepingByRoomUseCase(
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<CompleteHousekeepingByTokenInput, HousekeepingTicketResult>>(sp => new CompleteHousekeepingByTokenUseCase(
            sp.GetRequiredService<Rooms.Contracts.IRoomTokenResolver>(),
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<CreateHousekeepingByStaffInput, RequestHousekeepingResult>>(sp => new CreateHousekeepingByStaffUseCase(
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        services.AddScoped<IUseCase<CancelOpenTicketsForVisitInput, CancelOpenTicketsForVisitResult>>(sp => new CancelOpenTicketsForVisitUseCase(
            sp.GetRequiredService<IHousekeepingReader>(),
            sp.GetRequiredKeyedService<IRepository<HousekeepingTicket>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<HousekeepingEvent>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        // Validator module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<SetHousekeepingStatusInput>, SetHousekeepingStatusValidator>();
        services.AddTransient<IValidator<CompleteHousekeepingByTokenInput>, CompleteHousekeepingByTokenValidator>();

        return services;
    }
}
