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

        // Hasher cookie thiết bị (thuật toán thuần, stateless) — singleton thủ công (port không mang marker DI).
        services.AddSingleton<IGuestSessionKeyHasher, Sha256GuestSessionKeyHasher>();

        // Store aggregate session/visit (row-lock FOR UPDATE) — inject GuestAccessDbContext cụ thể (scoped).
        services.AddScoped<IGuestSessionStore, EfGuestSessionStore>();

        // Resolve use case: factory resolve keyed IUnitOfWork (guest_access) + cross-module Contracts. IUseCase
        // thường (tự quản transaction hẹp qua ExecuteInTransactionAsync — QR-AD-026).
        services.AddScoped<IUseCase<ResolveTokenInput, ResolveTokenResult>>(sp => new ResolveTokenUseCase(
            sp.GetRequiredService<IGuestSessionStore>(),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<IRoomTokenResolver>(),
            sp.GetRequiredService<IResortGuestConfigQuery>(),
            sp.GetRequiredService<IGuestSessionKeyHasher>(),
            sp.GetRequiredService<ITokenGenerator>(),
            sp.GetRequiredService<IClock>()));

        return services;
    }
}
