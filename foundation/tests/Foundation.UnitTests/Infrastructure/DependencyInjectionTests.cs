using System;
using Foundation.Application.Abstractions;
using Foundation.Application.Abstractions.Persistence;
using Foundation.Application.Abstractions.Security;
using Foundation.Application.Common;
using Foundation.Application.Identity;
using Foundation.Infrastructure.DependencyInjection;
using Foundation.Infrastructure.Security;
using Foundation.Infrastructure.Time;
using Foundation.SharedKernel.DependencyInjection;
using Foundation.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Foundation.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    // Fixture cố ý vi phạm: hiện thực 2 marker lifetime khác nhau (Req 2.6).
    private sealed class ConflictingService : IScopedService, ISingletonService;

    private static ServiceProvider BuildFoundationProvider()
    {
        var services = new ServiceCollection();

        // Options + store (app cung cấp) để hoàn tất đồ thị phụ thuộc.
        services.AddSingleton(new JwtOptions
        {
            SigningKey = "test-signing-key-0123456789-abcdefghijklmnop",
            Issuer = "it",
            Audience = "it",
            AccessTokenMinutes = 15,
        });
        services.AddSingleton(new PasswordHashingOptions());
        services.AddSingleton(new RefreshTokenOptions());
        services.AddScoped<IUserAuthStore, FakeUserAuthStore>();
        services.AddScoped<IRefreshTokenStore, FakeRefreshTokenStore>();
        services.AddScoped<IUnitOfWork, FakeUnitOfWork>();

        services.AddFoundationServices(
            typeof(Foundation.Application.AssemblyMarker).Assembly,
            typeof(Foundation.Infrastructure.AssemblyMarker).Assembly);

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact] // Req 2.1: marker → đăng ký đúng implementation
    public void Marker_services_should_resolve_to_expected_implementations()
    {
        using var provider = BuildFoundationProvider();

        Assert.IsType<CryptoTokenGenerator>(provider.GetRequiredService<ITokenGenerator>());
        Assert.IsType<Argon2idPasswordHasher>(provider.GetRequiredService<IPasswordHasher>());
        Assert.IsType<SystemDateTimeProvider>(provider.GetRequiredService<IDateTimeProvider>());
        Assert.IsType<Sha256RefreshTokenHasher>(provider.GetRequiredService<IRefreshTokenHasher>());
        Assert.IsType<JwtTokenService>(provider.GetRequiredService<IJwtTokenService>());
    }

    [Fact] // Req 2.1: Singleton = một instance; Scoped = mỗi scope một instance
    public void Lifetimes_should_match_markers()
    {
        using var provider = BuildFoundationProvider();

        Assert.Same(
            provider.GetRequiredService<ITokenGenerator>(),
            provider.GetRequiredService<ITokenGenerator>());

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var uc1 = scope1.ServiceProvider.GetRequiredService<IUseCase<LoginCommand, AuthTokens>>();
        var uc2 = scope2.ServiceProvider.GetRequiredService<IUseCase<LoginCommand, AuthTokens>>();

        Assert.IsType<ValidationUseCaseDecorator<LoginCommand, AuthTokens>>(uc1);
        Assert.NotSame(uc1, uc2);
    }

    [Fact] // Req 2.6: class có nhiều marker lifetime → fail-fast
    public void Conflicting_lifetime_markers_should_throw()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddFoundationServices(typeof(ConflictingService).Assembly));

        Assert.Contains(nameof(ConflictingService), ex.Message, StringComparison.Ordinal);
    }

    [Fact] // Fix #2: ICommandUseCase<> cũng được bọc validation (không chỉ IUseCase<,>)
    public void Command_use_cases_should_be_validation_decorated()
    {
        using var provider = BuildFoundationProvider();
        using var scope = provider.CreateScope();

        var useCase = scope.ServiceProvider.GetRequiredService<ICommandUseCase<LogoutCommand>>();

        Assert.IsType<ValidationCommandUseCaseDecorator<LogoutCommand>>(useCase);
    }
}
