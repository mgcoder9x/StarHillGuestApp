using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Rooms.Application;
using Rooms.Contracts;
using Rooms.Domain;
using Rooms.Infrastructure.Persistence;

namespace Rooms.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Rooms (mirror Identity/ResortConfig). KEYED persistence (P0-1 fix,
/// design §4.6): <see cref="RoomsDbContext"/> + repository <see cref="Room"/>/<see cref="RoomQrToken"/> + Unit of
/// Work đăng ký theo <see cref="PersistenceKey"/> → resolve đúng module (chống last-registration-wins). Use case
/// resolve port bằng cùng key qua factory. <see cref="IRoomTokenResolver"/>/<see cref="IQrService"/> đăng ký thủ
/// công (Contracts không mang marker DI); <see cref="EfRoomTokenResolver"/> inject RoomsDbContext CỤ THỂ (không cần
/// key). <see cref="RenderRoomQrPngUseCase"/> đọc cấu hình cross-module qua <c>IResortSettingsQuery</c> (Host ráp cả
/// hai module nên có sẵn ở composition root). Host gọi <c>AddBedrockCore</c> sau → bọc pipeline behaviors.
/// </summary>
public static class RoomsInfrastructureExtensions
{
    /// <summary>Module key persistence của Rooms (= schema <c>rooms</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = RoomsModule.PersistenceKey;

    public static IServiceCollection AddRoomsInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + Unit of Work + DB readiness check (schema "rooms"), KEYED theo PersistenceKey.
        // Rooms KHÔNG map Outbox/Inbox (chưa phát integration-event) nên không đăng ký capability đó.
        services.AddBedrockPersistence<RoomsDbContext>(PersistenceKey, configureDbContext);

        // Repository aggregate KEYED theo đúng RoomsDbContext (không dùng generic IRepository<> unkeyed global).
        services.AddBedrockRepository<RoomsDbContext, Room>(PersistenceKey);
        services.AddBedrockRepository<RoomsDbContext, RoomQrToken>(PersistenceKey);

        // Resolver token (read-only) inject RoomsDbContext cụ thể → auto-wire, không cần key.
        services.AddScoped<IRoomTokenResolver, EfRoomTokenResolver>();

        // Read admin nội-module (B-Rooms.4): list/detail phòng. Read-only → inject RoomsDbContext cụ thể (không keyed).
        services.AddScoped<IRoomQueries, EfRoomQueries>();

        // Query-port stats cho Host Dashboard (QR-AD-002, đếm phòng active). Contracts interface — Id trần.
        services.AddScoped<IRoomStatsQuery, EfRoomStatsQuery>();

        // QR render service (QRCoder) — thuần managed, stateless → singleton (thủ công, không auto-scan).
        services.AddSingleton<IQrService, QrCoderQrService>();

        // Use case: factory resolve repo/UoW bằng module key (mirror Identity). Void command (Update/ChangeStatus/
        // Delete) còn khai PersistenceKey → TransactionCommandUseCaseDecorator resolve cùng keyed UoW (nhất quán).
        services.AddScoped<IUseCase<CreateRoomInput, CreateRoomResult>>(sp => new CreateRoomUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RoomQrToken>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<ICurrentUser>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<ITokenGenerator>(),
            sp.GetRequiredService<IResortExistenceQuery>())); // P1(a): thẩm định ResortId (Host ráp cả 2 module).

        services.AddScoped<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>>(sp => new RotateRoomTokenUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RoomQrToken>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
            sp.GetRequiredService<ICurrentUser>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<ITokenGenerator>()));

        services.AddScoped<IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>>(sp => new RenderRoomQrPngUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IRepository<RoomQrToken>>(PersistenceKey),
            sp.GetRequiredService<IResortSettingsQuery>(),
            sp.GetRequiredService<IQrService>()));

        services.AddScoped<ICommandUseCase<UpdateRoomInput>>(sp => new UpdateRoomUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<ChangeRoomStatusInput>>(sp => new ChangeRoomStatusUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        services.AddScoped<ICommandUseCase<Guid>>(sp => new DeleteRoomUseCase(
            sp.GetRequiredKeyedService<IRepository<Room>>(PersistenceKey),
            sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey)));

        // Validator module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<CreateRoomInput>, CreateRoomValidator>();
        services.AddTransient<IValidator<UpdateRoomInput>, UpdateRoomValidator>();

        return services;
    }
}
