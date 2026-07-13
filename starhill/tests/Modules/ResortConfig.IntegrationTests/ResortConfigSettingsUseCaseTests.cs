using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application;
using ResortConfig.Contracts.Queries;
using ResortConfig.Infrastructure.DependencyInjection;
using ResortConfig.Infrastructure.Persistence;
using Xunit;

namespace ResortConfig.IntegrationTests;

/// <summary>
/// Kiểm <see cref="UpdateResortSettingsUseCase"/> (B-Config.3) trên SQLite in-memory (KHÔNG Docker) + validator
/// thuần. Update đổi field + GuestWebBaseUrl (đọc lại qua <see cref="IResortSettingsQuery"/>); settings chưa seed →
/// <c>not_found</c>; validator chặn URL http/không-absolute + range xấu (→ validation_error qua pipeline).
/// </summary>
public sealed class ResortConfigSettingsUseCaseTests
{
    private sealed class StubCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public Guid? TenantId => null;
        public Guid? SessionId => null;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddResortConfigInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResortConfigDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static UpdateResortSettingsInput ValidInput(string? baseUrl = "https://guest.example.com") =>
        new(
            FaqEnabled: false,
            ChatEnabled: true,
            HousekeepingEnabled: false,
            RequireRuleAckForFaq: true,
            RequireRuleAckForChat: true,
            RequireRuleAckForHousekeeping: false,
            PortalWindowMinutes: 45,
            VisitIdleExpiryHours: 48,
            GuestWebBaseUrl: baseUrl,
            MaxMessageLength: 500,
            MessageRateLimitPerMinute: 20,
            HousekeepingRateLimitPerHour: 6);

    [Fact]
    public async Task Update_changes_settings_fields()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ResortConfigSeeder>().SeedAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateResortSettingsInput>>();
            var result = await uc.ExecuteAsync(ValidInput());
            Assert.True(result.IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var snapshot = await scope.ServiceProvider.GetRequiredService<IResortSettingsQuery>().GetAsync();
            Assert.NotNull(snapshot);
            Assert.False(snapshot!.FaqEnabled);
            Assert.True(snapshot.RequireRuleAckForFaq);
            Assert.Equal(45, snapshot.PortalWindowMinutes);
            Assert.Equal(48, snapshot.VisitIdleExpiryHours);
            Assert.Equal("https://guest.example.com", snapshot.GuestWebBaseUrl);
            Assert.Equal(500, snapshot.MaxMessageLength);
        }
    }

    [Fact]
    public async Task Update_without_seeded_settings_returns_not_found()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateResortSettingsInput>>();
        var result = await uc.ExecuteAsync(ValidInput());

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error.Code);
    }

    [Theory]
    [InlineData("http://guest.example.com")] // không https.
    [InlineData("guest.example.com")]         // không absolute.
    [InlineData("not a url")]
    public void Validator_rejects_invalid_base_url(string badUrl)
    {
        var validator = new UpdateResortSettingsValidator();
        var result = validator.Validate(ValidInput(badUrl));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_accepts_null_base_url_and_valid_https()
    {
        var validator = new UpdateResortSettingsValidator();
        Assert.True(validator.Validate(ValidInput(baseUrl: null)).IsValid);
        Assert.True(validator.Validate(ValidInput("https://ok.example.com")).IsValid);
    }

    [Fact]
    public void Validator_rejects_out_of_range_numbers()
    {
        var validator = new UpdateResortSettingsValidator();
        var bad = ValidInput() with { PortalWindowMinutes = 0, VisitIdleExpiryHours = 0, MaxMessageLength = 0 };
        Assert.False(validator.Validate(bad).IsValid);
    }
}
