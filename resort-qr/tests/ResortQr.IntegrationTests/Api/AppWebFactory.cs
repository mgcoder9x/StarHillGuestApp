using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ResortQr.Application.Abstractions.Security;
using ResortQr.Application.Identity;
using ResortQr.Application.Rooms;
using ResortQr.Domain.Identity;
using ResortQr.Domain.Resorts;
using ResortQr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ResortQr.IntegrationTests.Api;

/// <summary>
/// WebApplicationFactory chạy Program THẬT nhưng wire <see cref="AppDbContext"/> trên SQLite in-memory
/// (DB quan hệ THẬT, Docker-free) + seed resort/settings/admin/staff. Harness HTTP end-to-end dùng lại cho
/// mọi wave (auth JWT thật → policy → use case → EF). Program bỏ qua Npgsql vì KHÔNG có ConnectionStrings:Postgres.
/// </summary>
public sealed class AppWebFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin#12345";
    public const string StaffEmail = "staff@test.local";
    public const string StaffPassword = "Staff#12345";

    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=True");

    public AppWebFactory() => _connection.Open();

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
            // Wire AppDbContext trên SQLite (thay Npgsql của production) + tầng nền + store/query coupled DbContext.
            services.AddDbContext<AppDbContext>(o =>
                o.UseSqlite(_connection).UseSnakeCaseNamingConvention());
            services.AddResortQrPersistence<AppDbContext>();
            services.AddScoped<IUserAuthStore, AppUserAuthStore>();
            services.AddScoped<IRoomQueries, EfRoomQueries>();

            // Tạo schema + seed khi host khởi động.
            services.AddHostedService<DbInitializer>();
        });
    }

    /// <summary>Login lấy access token (JWT thật) cho email/password đã seed.</summary>
    public async Task<string> GetAccessTokenAsync(string email, string password)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.AccessToken;
    }

    /// <summary>HttpClient kèm Bearer token của một role đã seed.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var token = await GetAccessTokenAsync(email, password);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);

    /// <summary>Ensure schema + seed resort/settings/admin/staff khi khởi động host.</summary>
    private sealed class DbInitializer : IHostedService
    {
        private readonly IServiceProvider _services;

        public DbInitializer(IServiceProvider services) => _services = services;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            await db.Database.EnsureCreatedAsync(cancellationToken);

            if (await db.Resorts.AnyAsync(cancellationToken))
            {
                return;
            }

            var resort = new Resort { Name = "Test Resort", Timezone = "Asia/Ho_Chi_Minh", CreatedAt = DateTimeOffset.UnixEpoch };
            db.Resorts.Add(resort);
            db.ResortSettingsSet.Add(new ResortSettings
            {
                ResortId = resort.Id,
                GuestWebBaseUrl = "https://guest.test.local",
            });
            db.AppUsers.Add(NewUser(resort.Id, AdminEmail, hasher.Hash(AdminPassword), UserRole.Admin));
            db.AppUsers.Add(NewUser(resort.Id, StaffEmail, hasher.Hash(StaffPassword), UserRole.Staff));

            // Ngôn ngữ (khớp ResortSeeder thật): en mặc định + vi/ko/zh.
            (string Code, string Name, bool IsDefault)[] languages =
            [
                ("en", "English", true),
                ("vi", "Tiếng Việt", false),
                ("ko", "한국어", false),
                ("zh", "中文", false),
            ];
            var sortOrder = 0;
            foreach (var (code, name, isDefault) in languages)
            {
                db.ResortLanguages.Add(new ResortLanguage
                {
                    ResortId = resort.Id,
                    Code = code,
                    DisplayName = name,
                    IsEnabled = true,
                    IsDefault = isDefault,
                    SortOrder = sortOrder++,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static AppUser NewUser(Guid resortId, string email, string hash, UserRole role) => new()
        {
            ResortId = resortId,
            Email = email,
            DisplayName = role.ToString(),
            PasswordHash = hash,
            Role = role,
            IsActive = true,
        };
    }
}
