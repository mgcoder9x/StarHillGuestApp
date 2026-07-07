using System;
using System.Collections.Generic;
using ResortQr.Application.Abstractions.Persistence;
using ResortQr.Application.Abstractions.Security;
using ResortQr.Application.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResortQr.IntegrationTests.Auth;

/// <summary>
/// WebApplicationFactory chạy Program THẬT của base, nhưng cấp store in-memory (thay EF/DB) + config test.
/// Chứng minh pipeline auth (Options validate, DI, JWT Bearer, ProblemDetails, endpoint) hoạt động end-to-end
/// qua HTTP mà KHÔNG cần Postgres/Docker.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string DefaultRole = "Admin";
    private static readonly string[] DefaultRoles = [DefaultRole];

    public InMemoryUserAuthStore Users { get; } = new();
    public InMemoryRefreshTokenStore RefreshTokens { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "integration-test-signing-key-0123456789-abcdef",
                ["Jwt:Issuer"] = "ResortQr-it",
                ["Jwt:Audience"] = "ResortQr-it",
                ["Jwt:AccessTokenMinutes"] = "15",
                // Tham số hash NHỎ để test nhanh (không phải cấu hình prod).
                ["PasswordHashing:MemoryKib"] = "1024",
                ["PasswordHashing:Iterations"] = "1",
                ["PasswordHashing:DegreeOfParallelism"] = "1",
                ["PasswordHashing:SaltSize"] = "16",
                ["PasswordHashing:HashSize"] = "32",
                ["RefreshToken:RefreshTokenDays"] = "30",
                ["Security:AllowedCorsOrigins:0"] = "https://allowed.example",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Base để trống các store/UoW (cần DB) → integration test cấp bản in-memory (singleton giữ state).
            services.AddSingleton<IUserAuthStore>(Users);
            services.AddSingleton<IRefreshTokenStore>(RefreshTokens);
            services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
        });
    }

    /// <summary>Seed một user dùng CHÍNH hasher đã cấu hình của app (đảm bảo verify khớp).</summary>
    public Guid SeedUser(string email, string password, bool active = true)
    {
        var hasher = Services.GetRequiredService<IPasswordHasher>();
        var userId = Guid.CreateVersion7();
        Users.Seed(new AuthenticatedUser(userId, email, hasher.Hash(password), DefaultRoles, active));
        return userId;
    }
}
