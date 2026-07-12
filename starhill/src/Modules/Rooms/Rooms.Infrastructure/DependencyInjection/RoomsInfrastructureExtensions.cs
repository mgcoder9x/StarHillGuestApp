using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rooms.Application;
using Rooms.Contracts;
using Rooms.Infrastructure.Persistence;

namespace Rooms.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure composition module Rooms (mirror ResortConfig/Identity). Đăng ký: DbContext + persistence
/// nền (repo/UoW/DB health) qua <c>AddBedrockPersistence</c> + <see cref="IRoomTokenResolver"/> (thủ công scoped —
/// Contracts không mang marker DI) + use case CRUD/rotate/render + <see cref="IQrService"/> (QRCoder, singleton) +
/// validator FluentValidation. Use case scoped (dùng chung scope/DbContext với UoW; Host gọi <c>AddBedrockCore</c>
/// sau → bọc pipeline). <see cref="RenderRoomQrPngUseCase"/> phụ thuộc <c>IResortSettingsQuery</c> (đăng ký ở
/// <c>AddResortConfigInfrastructure</c> — Host ráp cả hai module nên có sẵn ở composition root).
/// </summary>
public static class RoomsInfrastructureExtensions
{
    public static IServiceCollection AddRoomsInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddBedrockPersistence<RoomsDbContext>(configureDbContext);
        services.AddScoped<IRoomTokenResolver, EfRoomTokenResolver>();

        // QR render service (QRCoder) — thuần managed, stateless → singleton (thủ công, không auto-scan).
        services.AddSingleton<IQrService, QrCoderQrService>();

        // Use case CRUD/rotate/render (thủ công như Identity — Bedrock KHÔNG auto-scan use case module).
        services.AddScoped<IUseCase<CreateRoomInput, CreateRoomResult>, CreateRoomUseCase>();
        services.AddScoped<IUseCase<RotateRoomTokenInput, RotateRoomTokenResult>, RotateRoomTokenUseCase>();
        services.AddScoped<IUseCase<RenderRoomQrPngInput, RenderRoomQrPngResult>, RenderRoomQrPngUseCase>();
        services.AddScoped<ICommandUseCase<UpdateRoomInput>, UpdateRoomUseCase>();
        services.AddScoped<ICommandUseCase<ChangeRoomStatusInput>, ChangeRoomStatusUseCase>();
        services.AddScoped<ICommandUseCase<Guid>, DeleteRoomUseCase>();

        // Validator module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<CreateRoomInput>, CreateRoomValidator>();
        services.AddTransient<IValidator<UpdateRoomInput>, UpdateRoomValidator>();

        return services;
    }
}
