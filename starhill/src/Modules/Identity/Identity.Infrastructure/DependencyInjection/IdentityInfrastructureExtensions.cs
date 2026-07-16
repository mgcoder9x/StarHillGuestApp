using Bedrock.Application.Messaging;
using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using FluentValidation;
using Identity.Application.RefreshToken;
using Identity.Contracts;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.DependencyInjection;

/// <summary>
/// Nửa-Infrastructure của composition module Identity (DV-013 — tách khỏi nửa-Api để giữ Api⊥Infra I7).
/// KEYED persistence (P0-1 fix, design §4.6): đăng ký <see cref="IdentityDbContext"/> + capability Outbox/Inbox/
/// RefreshTokens theo <see cref="PersistenceKey"/> nên mọi port phụ thuộc context resolve đúng module (không còn
/// last-registration-wins giữa các module). Use case resolve port bằng cùng key qua factory. Host gọi method này
/// + <c>AddIdentityApi</c> + <c>AddBedrockCore</c> (decorate pipeline sau khi use case đã đăng ký).
/// </summary>
public static class IdentityInfrastructureExtensions
{
    /// <summary>Module key persistence của Identity (= schema <c>identity</c>) — nguồn duy nhất ở Contracts.</summary>
    public const string PersistenceKey = IdentityModule.PersistenceKey;

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // DbContext + persistence nền + capability khớp IdentityDbContext.OnModelCreating (schema "identity":
        // AddOutboxInbox + AddRefreshTokens). Tất cả KEYED theo PersistenceKey.
        services.AddBedrockPersistence<IdentityDbContext>(PersistenceKey, configureDbContext);
        services.AddBedrockOutbox<IdentityDbContext>(PersistenceKey);
        services.AddBedrockInbox<IdentityDbContext>(PersistenceKey);
        services.AddBedrockRefreshTokens<IdentityDbContext>(PersistenceKey);
        // F.1a: repository IdentityUser KEYED (login lookup + seeder). Mirror repo keyed các module khác.
        services.AddBedrockRepository<IdentityDbContext, Identity.Domain.IdentityUser>(PersistenceKey);
        // F.1c: seeder admin idempotent (scoped — dùng chung scope/DbContext; Host gọi sau migrate, gated dev/config).
        services.AddScoped<Identity.Infrastructure.Persistence.IdentityUserSeeder>();
        // F.1a: repo user (login lookup theo username, seeder tạo admin) — KEYED theo IdentityDbContext.
        services.AddBedrockRepository<IdentityDbContext, Identity.Domain.IdentityUser>(PersistenceKey);

        // Factory resolve toàn bộ persistence port bằng module key (không có global PlatformDbContext alias unkeyed).
        services.AddScoped<IUseCase<RefreshTokenCommand, RefreshTokenResult>>(sp =>
            new RefreshAccessTokenUseCase(
                sp.GetRequiredKeyedService<IRefreshTokenStore>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<ITokenGenerator>(),
                sp.GetRequiredService<IJwtTokenService>(),
                sp.GetRequiredKeyedService<IOutboxWriter>(PersistenceKey)));

        // F.1b: LoginUseCase — factory resolve repo user + refresh store + UoW keyed + port bảo mật base (Argon2/JWT/token-gen).
        services.AddScoped<IUseCase<Identity.Application.Login.LoginCommand, Identity.Application.Login.LoginResult>>(sp =>
            new Identity.Application.Login.LoginUseCase(
                sp.GetRequiredKeyedService<IRepository<Identity.Domain.IdentityUser>>(PersistenceKey),
                sp.GetRequiredKeyedService<IRefreshTokenStore>(PersistenceKey),
                sp.GetRequiredKeyedService<IUnitOfWork>(PersistenceKey),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<ITokenGenerator>(),
                sp.GetRequiredService<IJwtTokenService>(),
                sp.GetRequiredService<IPasswordHasher>()));

        // Validator của module (ValidationUseCaseDecorator nhận qua IEnumerable<IValidator<TInput>>).
        services.AddTransient<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddTransient<IValidator<Identity.Application.Login.LoginCommand>, Identity.Application.Login.LoginCommandValidator>();

        return services;
    }
}
