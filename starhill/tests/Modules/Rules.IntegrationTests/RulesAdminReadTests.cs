using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Rules.Application;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// D-Rules.3b (Req 8.4) — admin PREVIEW Draft "như khách" (render + i18n fallback CP5, KHÔNG publish) + lịch sử
/// PUBLICATION (metadata, Version giảm dần). SQLite in-memory (read + i18n provider-agnostic — KHÔNG cần Docker).
/// </summary>
public sealed class RulesAdminReadTests
{
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

    private sealed class StubGuestConfigQuery : IResortGuestConfigQuery
    {
        public ResortGuestConfig? Config { get; set; }
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(Config);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId, params string[] langs) => new(
        resortId, "Star Hill", null, langs, langs[0], true, true, true, false, false, false, 30, 24);

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITranslationResolver, TranslationResolver>();
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddRulesInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection, config);
    }

    private static async Task SeedDraftAsync(ServiceProvider provider, Guid resortId, string key, params (string Lang, string Title, string Body)[] translations)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var set = await db.RuleSets.FirstOrDefaultAsync(rs => rs.ResortId == resortId);
        if (set is null)
        {
            set = new RuleSet { ResortId = resortId, UpdatedAt = DateTimeOffset.UnixEpoch };
            db.RuleSets.Add(set);
        }

        var section = new RuleSection { RuleSetId = set.Id, Key = key, SortOrder = 1 };
        db.RuleSections.Add(section);
        foreach (var (lang, title, body) in translations)
        {
            db.RuleSectionTranslations.Add(new RuleSectionTranslation
            {
                RuleSectionId = section.Id,
                LanguageCode = lang,
                Title = title,
                BodyHtmlSanitized = body,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedPublicationAsync(ServiceProvider provider, Guid resortId, int version, bool isCurrent)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        db.RulePublications.Add(new RulePublication
        {
            ResortId = resortId,
            Version = version,
            PublishedAt = DateTimeOffset.UnixEpoch.AddDays(version),
            IsCurrent = isCurrent,
            ChangeNote = $"v{version}",
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Preview_renders_draft_with_i18n_fallback()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi"); // default en
        await SeedDraftAsync(provider, resortId, "welcome", ("en", "Welcome", "<p>hi</p>")); // chỉ en

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>>();
        var result = await uc.ExecuteAsync(new GetDraftPreviewInput(resortId, "vi")); // yêu cầu vi (không có)

        Assert.True(result.IsSuccess);
        Assert.Equal("vi", result.Value.Language);
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal("welcome", section.Key);
        Assert.Equal("Welcome", section.Title);   // fallback en
        Assert.True(section.IsFallback);
    }

    [Fact]
    public async Task Preview_with_no_draft_returns_empty()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>>();
        var result = await uc.ExecuteAsync(new GetDraftPreviewInput(resortId, "en"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sections);
    }

    [Fact]
    public async Task Preview_missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        config.Config = null;

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetDraftPreviewInput, GetDraftPreviewResult>>();
        var result = await uc.ExecuteAsync(new GetDraftPreviewInput(Guid.CreateVersion7(), "en"));

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task History_lists_publications_newest_first()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");
        await SeedPublicationAsync(provider, resortId, version: 1, isCurrent: false);
        await SeedPublicationAsync(provider, resortId, version: 2, isCurrent: true);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>>();
        var result = await uc.ExecuteAsync(new GetPublicationHistoryInput(resortId));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Publications.Count);
        Assert.Equal(2, result.Value.Publications[0].Version); // Version giảm dần.
        Assert.True(result.Value.Publications[0].IsCurrent);
        Assert.Equal(1, result.Value.Publications[1].Version);
        Assert.False(result.Value.Publications[1].IsCurrent);
    }

    [Fact]
    public async Task History_empty_when_never_published()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetPublicationHistoryInput, GetPublicationHistoryResult>>();
        var result = await uc.ExecuteAsync(new GetPublicationHistoryInput(resortId));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Publications);
    }
}
