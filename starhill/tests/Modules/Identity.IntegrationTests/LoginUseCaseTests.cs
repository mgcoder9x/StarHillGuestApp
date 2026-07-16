using Bedrock.Application.Ports.Security;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Infrastructure.DependencyInjection;
using Identity.Application.Login;
using Identity.Domain;
using Identity.Infrastructure.DependencyInjection;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identity.IntegrationTests;

/// <summary>
/// F.1b — <c>LoginUseCase</c> đầu-cuối với crypto THẬT (Argon2id + JWT key-ring base qua <c>AddBedrockSecurity</c>) +
/// DB THẬT (Postgres/migration). Chứng minh: đúng credential → phát access-token mang claim <c>role</c> (admin) +
/// refresh-token + hạn; MỌI thất bại (sai pass / user lạ / inactive) → CÙNG mã <c>identity.invalid_credentials</c>
/// (chống enumeration). SKIP nếu thiếu Docker (N-067). Cùng collection serialize Postgres.
/// </summary>
[Collection(IdentityIntegrationDefinition.Name)]
public sealed class LoginUseCaseTests : IAsyncLifetime
{
    private const string Password = "Sup3r-Secret-Pw!";

    private PostgreSqlContainer _container = null!;
    private bool _available;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync().ConfigureAwait(false);
            _available = true;
        }
#pragma warning disable CA1031 // CỐ Ý: thiếu Docker → skip.
        catch (Exception)
#pragma warning restore CA1031
        {
            _available = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_available)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private static IConfiguration JwtConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ActiveKid"] = "k1",
            ["Jwt:Issuer"] = "starhill-test",
            ["Jwt:Audience"] = "starhill-test",
            ["Jwt:Keys:0:Kid"] = "k1",
            ["Jwt:Keys:0:Secret"] = Convert.ToBase64String(new byte[32]),
        }).Build();

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock>(new FixedClock()); // TRƯỚC AddBedrockSecurity (TryAdd không override).
        services.AddBedrockSecurity(JwtConfig());          // Argon2id + JWT + token-gen THẬT.
        services.AddIdentityInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task MigrateAndSeedAsync(ServiceProvider provider, bool isActive)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new IdentityUser
        {
            Username = "admin",
            PasswordHash = hasher.Hash(Password),
            Role = UserRole.Admin,
            IsActive = isActive,
            DisplayName = "Administrator",
            CreatedAt = DateTimeOffset.UnixEpoch,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Bedrock.Domain.Results.Result<LoginResult>> LoginAsync(
        ServiceProvider provider, string username, string password)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<LoginCommand, LoginResult>>();
        return await uc.ExecuteAsync(new LoginCommand(username, password));
    }

    [SkippableFact]
    public async Task Correct_credentials_issue_access_token_with_role_and_refresh()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await MigrateAndSeedAsync(provider, isActive: true);

        var result = await LoginAsync(provider, "admin", Password);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
        Assert.True(result.Value.RefreshTokenExpiresAt > new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        // Access-token mang claim role=admin (JWT-native "role") → policy RequireAdmin/RequireStaff authorize đúng.
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.Value.AccessToken);
        Assert.Equal("admin", jwt.GetClaim("role").Value);

        // Refresh-token đã persist (login khởi tạo family) → đăng nhập không rỗng store.
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredKeyedService<IRefreshTokenStore>(IdentityInfrastructureExtensions.PersistenceKey);
        var snapshot = await store.GetByHashAsync(RefreshHash(result.Value.RefreshToken));
        Assert.NotNull(snapshot);
    }

    [SkippableFact]
    public async Task Wrong_password_returns_invalid_credentials()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await MigrateAndSeedAsync(provider, isActive: true);

        var result = await LoginAsync(provider, "admin", "wrong-password");

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
    }

    [SkippableFact]
    public async Task Unknown_user_returns_invalid_credentials()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await MigrateAndSeedAsync(provider, isActive: true);

        var result = await LoginAsync(provider, "ghost", Password);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
    }

    [SkippableFact]
    public async Task Inactive_user_cannot_login_even_with_correct_password()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await MigrateAndSeedAsync(provider, isActive: false);

        var result = await LoginAsync(provider, "admin", Password);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
    }

    [SkippableFact]
    public async Task Username_lookup_is_case_insensitive_via_normalization()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await MigrateAndSeedAsync(provider, isActive: true);

        // Seed lưu "admin"; đăng nhập "ADMIN" → use case chuẩn hóa lower → khớp.
        var result = await LoginAsync(provider, "ADMIN", Password);

        Assert.True(result.IsSuccess);
    }

    private static string RefreshHash(string raw) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}
