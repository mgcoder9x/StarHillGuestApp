using Bedrock.Application.UseCases;
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
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + repo/UoW/outbox-writer/refresh-store + DB readiness check (schema "identity").
        services.AddBedrockPersistence<IdentityDbContext>(configureDbContext);

        // Use case rotation (scoped — dùng chung scope/DbContext với UoW). Host gọi AddBedrockCore sau → bọc pipeline.
        services.AddScoped<IUseCase<RefreshTokenCommand, RefreshTokenResult>, RefreshAccessTokenUseCase>();

        // Validator của module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<RefreshTokenCommand>>).
        services.AddTransient<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();

        return services;
    }
}
