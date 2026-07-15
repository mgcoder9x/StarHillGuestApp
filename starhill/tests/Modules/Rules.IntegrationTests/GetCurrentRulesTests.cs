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
/// CP5 (Req 2/2.5) — guest đọc bản nội quy HIỆN HÀNH với i18n fallback qua <c>ITranslationResolver</c> THẬT
/// (ResortConfig.Application) + <c>GetCurrentRulesUseCase</c> + read-model publication THẬT. SQLite in-memory
/// (read + i18n provider-agnostic — không cần Docker). Phủ: exact-match; thiếu bản dịch→fallback default (IsFallback);
/// ngôn ngữ lạ→default; section không có bản dịch nào→IsMissing; chưa publish→rules_unavailable; config null→
/// configuration_unavailable; giữ thứ tự SortOrder.
/// </summary>
public sealed class GetCurrentRulesTests
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
        resortId, "Star Hill", null, langs, langs[0],
        true, true, true, false, false, false, 30, 24);

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITranslationResolver, TranslationResolver>(); // THẬT (không stub logic i18n).
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

    /// <summary>Seed một publication IsCurrent với các section + translation cho trước.</summary>
    private static async Task SeedPublicationAsync(
        ServiceProvider provider, Guid resortId,
        params (string Key, int SortOrder, (string Lang, string Title, string Body)[] Translations)[] sections)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var pub = new RulePublication
        {
            ResortId = resortId,
            Version = 1,
            PublishedAt = DateTimeOffset.UnixEpoch,
            IsCurrent = true,
        };
        db.RulePublications.Add(pub);

        foreach (var (key, sortOrder, translations) in sections)
        {
            var pubSection = new RulePublicationSection
            {
                RulePublicationId = pub.Id,
                Key = key,
                SortOrder = sortOrder,
            };
            db.RulePublicationSections.Add(pubSection);

            foreach (var (lang, title, body) in translations)
            {
                db.RulePublicationSectionTranslations.Add(new RulePublicationSectionTranslation
                {
                    RulePublicationSectionId = pubSection.Id,
                    LanguageCode = lang,
                    Title = title,
                    BodyHtmlSanitized = body,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Bedrock.Domain.Results.Result<GetCurrentRulesResult>> GetAsync(
        ServiceProvider provider, Guid resortId, string? lang)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetCurrentRulesInput, GetCurrentRulesResult>>();
        return await uc.ExecuteAsync(new GetCurrentRulesInput(resortId, lang));
    }

    [Fact]
    public async Task Exact_language_match_returns_content_without_fallback()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi");
        await SeedPublicationAsync(provider, resortId,
            ("welcome", 1, [("en", "Welcome", "<p>Hello</p>"), ("vi", "Chào", "<p>Xin chào</p>")]));

        var result = await GetAsync(provider, resortId, "vi");

        Assert.True(result.IsSuccess);
        Assert.Equal("vi", result.Value.Language);
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal("Chào", section.Title);
        Assert.Equal("vi", section.ResolvedLanguage);
        Assert.False(section.IsFallback);
        Assert.False(section.IsMissing);
    }

    [Fact]
    public async Task Missing_translation_falls_back_to_default_language()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi"); // default = en
        await SeedPublicationAsync(provider, resortId,
            ("welcome", 1, [("en", "Welcome", "<p>Hello</p>")])); // CHỈ có en

        var result = await GetAsync(provider, resortId, "vi"); // yêu cầu vi (không có)

        Assert.True(result.IsSuccess);
        Assert.Equal("vi", result.Value.Language); // ngôn ngữ hiển thị đã match
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal("Welcome", section.Title);          // nội dung fallback en
        Assert.Equal("en", section.ResolvedLanguage);
        Assert.True(section.IsFallback);
        Assert.False(section.IsMissing);
    }

    [Fact]
    public async Task Unknown_requested_language_uses_default()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi");
        await SeedPublicationAsync(provider, resortId,
            ("welcome", 1, [("en", "Welcome", "<p>Hello</p>")]));

        var result = await GetAsync(provider, resortId, "zz"); // không nằm trong enabled

        Assert.True(result.IsSuccess);
        Assert.Equal("en", result.Value.Language); // MatchSupported → default en
        var section = Assert.Single(result.Value.Sections);
        Assert.Equal("Welcome", section.Title);
        Assert.False(section.IsFallback); // requested đã là en (default) → found, không fallback
    }

    [Fact]
    public async Task Section_without_any_translation_is_marked_missing()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");
        await SeedPublicationAsync(provider, resortId, ("orphan", 1, []));

        var result = await GetAsync(provider, resortId, "en");

        Assert.True(result.IsSuccess);
        var section = Assert.Single(result.Value.Sections);
        Assert.True(section.IsMissing);
        Assert.Null(section.Title);
        Assert.Null(section.BodyHtmlSanitized);
    }

    [Fact]
    public async Task Sections_are_returned_in_sort_order()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");
        await SeedPublicationAsync(provider, resortId,
            ("checkout", 2, [("en", "Checkout", "<p>bye</p>")]),
            ("welcome", 1, [("en", "Welcome", "<p>hi</p>")]));

        var result = await GetAsync(provider, resortId, "en");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Sections.Count);
        Assert.Equal("welcome", result.Value.Sections[0].Key); // SortOrder 1 trước
        Assert.Equal("checkout", result.Value.Sections[1].Key);
    }

    [Fact]
    public async Task No_publication_returns_rules_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");

        var result = await GetAsync(provider, resortId, "en");

        Assert.False(result.IsSuccess);
        Assert.Equal("rules_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = null; // fail-closed
        await SeedPublicationAsync(provider, resortId, ("welcome", 1, [("en", "W", "<p>x</p>")]));

        var result = await GetAsync(provider, resortId, "en");

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }
}
