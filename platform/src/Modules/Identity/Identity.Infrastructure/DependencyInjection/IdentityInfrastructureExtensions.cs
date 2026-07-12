using Bedrock.Application.UseCases;
using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Identity.Application.RefreshToken;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure của composition module Identity (DV-013 — tách khỏi nửa-Api để giữ Api⊥Infra I7).
/// Đăng ký: <see cref="IdentityDbContext"/> + persistence nền (repo/UoW/outbox/refresh-store/DB health) qua
/// <c>AddBedrockPersistence</c>, use case rotation, và validator của module. Host gọi method này + <c>AddIdentityApi</c>
/// + <c>AddBedrockCore</c> (decorate pipeline sau khi use case đã đăng ký).
/// </summary>
public static class IdentityInfrastructureExtensions
{
    public const string PersistenceKey = "identity";

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + repo/UoW/outbox-writer/refresh-store + DB readiness check (schema "identity").
        services.AddBedrockPersistence<IdentityDbContext>(PersistenceKey, configureDbContext);

        // Factory resolve toàn bộ persistence port bằng module key. Không có global PlatformDbContext alias nên
        // module thứ hai không thể làm Identity trỏ nhầm context theo registration order (A-01).
        services.AddScoped<IUseCase<RefreshTokenCommand, RefreshTokenResult>>(sp =>
            new RefreshAccessTokenUseCase(
                sp.GetRequiredKeyedService<IRefreshTokenStore>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<ITokenGenerator>(),
                sp.GetRequiredService<IJwtTokenService>(),
                sp.GetRequiredKeyedService<IOutboxWriter>(PersistenceKey)));

        // Validator của module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<RefreshTokenCommand>>).
        services.AddTransient<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();

        return services;
    }
}
