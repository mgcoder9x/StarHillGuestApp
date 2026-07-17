using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using GuestAccess.Application;
using GuestAccess.Contracts;
using GuestAccess.Infrastructure.Persistence;
using GuestAccess.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Contracts;

namespace GuestAccess.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module GuestAccess (mirror Identity/ResortConfig/Rooms). KEYED persistence
/// (P0-1 fix, design §4.6): <see cref="GuestAccessDbContext"/> + Unit of Work đăng ký theo
/// <see cref="GuestAccessModule.PersistenceKey"/> → resolve đúng module (chống last-registration-wins). C-GA.2b:
/// thêm hasher (SHA-256, singleton), <see cref="IGuestSessionStore"/> (scoped, row-lock) và
/// <see cref="ResolveTokenUseCase"/> (factory resolve keyed IUnitOfWork + cross-module Contracts). Resolve TỰ quản
/// transaction hẹp (không ITransactionalUseCase). GuestAccess KHÔNG map Outbox/Inbox tới khi cascade
/// GuestVisitEnded có consumer (QR-AD-027).
/// </summary>
public static class GuestAccessInfrastructureExtensions
{
    /// <summary>Module key persistence của GuestAccess (= schema <c>guest_access</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = GuestAccessModule.PersistenceKey;

    public static IServiceCollection AddGuestAccessInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "guest_access"), KEYED theo PersistenceKey.
        services.AddBedrockPersistence<GuestAccessDbContext>(PersistenceKey, configureDbContext);

        // C-GA.5: Outbox producer (phát GuestVisitEnded) + Inbox store (consume cascade idempotent) KEYED theo module.
        // Cần AddOutboxInbox ở GuestAccessDbContext.OnModelCreating (đã có). Startup guard P1-15: khi messaging TẮT,
        // producer không có drainer → boot chặn TRỪ KHI AllowOutboxWithoutDispatcher=true (compose/smoke đã đặt).
        services.AddBedrockOutbox<GuestAccessDbContext>(PersistenceKey);
        services.AddBedrockInbox<GuestAccessDbContext>(PersistenceKey);

        // Hasher cookie thiết bị (thuật toán thuần, stateless) — singleton thủ công (port không mang marker DI).
        services.AddSingleton<IGuestSessionKeyHasher, Sha256GuestSessionKeyHasher>();

        // Store aggregate session/visit (row-lock FOR UPDATE) — inject GuestAccessDbContext cụ thể (scoped).
        services.AddScoped<IGuestSessionStore, EfGuestSessionStore>();

        // Current-guest-context resolver (C-GA.4 — QR-AD-032): port cross-module cho Rules/Faq/Concierge/Housekeeping.
        // Factory resolve keyed IUnitOfWork (guest_access) cho TouchAsync — cùng scope nên cùng GuestAccessDbContext instance.
        services.AddScoped<ICurrentGuestContextResolver>(sp => new EfCurrentGuestContextResolver(
            sp.GetRequiredService<GuestAccessDbContext>(),
            sp.GetRequiredService<IResortGuestConfigQuery>(),
            sp.GetRequiredService<IGuestSessionKeyHasher>(),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IClock>()));

        // Resolve use case: factory resolve keyed IUnitOfWork (guest_access) + cross-module Contracts. IUseCase
        // thường (tự quản transaction hẹp qua ExecuteInTransactionAsync — QR-AD-026).
        services.AddScoped<IUseCase<ResolveTokenInput, ResolveTokenResult>>(sp => new ResolveTokenUseCase(
            sp.GetRequiredService<IGuestSessionStore>(),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IRoomTokenResolver>(),
            sp.GetRequiredService<IResortGuestConfigQuery>(),
            sp.GetRequiredService<IGuestSessionKeyHasher>(),
            sp.GetRequiredService<ITokenGenerator>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredKeyedService<IOutboxWriter>(PersistenceKey)));

        return services;
    }
}
