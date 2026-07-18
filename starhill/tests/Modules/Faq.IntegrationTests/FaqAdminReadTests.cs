using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Faq.Application;
using Faq.Domain;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Xunit;

namespace Faq.IntegrationTests;

public sealed class FaqAdminReadTests
{
    private static readonly string[] EnabledLanguages = ["en", "vi", "ko"];
    private static readonly string[] MissingVietnameseAndKorean = ["vi", "ko"];
    private static readonly string[] MissingEnglishAndKorean = ["en", "ko"];

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
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) =>
            Task.FromResult(Config);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId) => new(
        resortId, "Star Hill", null, ["en", "vi", "ko"], "en",
        true, true, true, false, false, false, 30, 24);

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
        services.AddFaqInfrastructure(options => options.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection, config);
    }

    [Fact]
    public async Task Admin_tree_includes_inactive_nodes_hierarchy_and_raw_translations()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId);

        Guid categoryId;
        Guid categoryTranslationId;
        Guid rootId;
        Guid rootTranslationId;
        Guid childId;
        Guid childTranslationId;
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var category = new FaqCategory
            {
                ResortId = resortId,
                Key = "arrival",
                SortOrder = 3,
                IsActive = false,
                RowVersion = 5,
            };
            var categoryTranslation = new FaqCategoryTranslation
            {
                FaqCategoryId = category.Id,
                LanguageCode = "en",
                Name = "Arrival",
                RowVersion = 7,
            };
            var root = new FaqItem
            {
                ResortId = resortId,
                CategoryId = category.Id,
                SortOrder = 1,
                IsActive = false,
                RowVersion = 11,
            };
            var rootTranslation = new FaqItemTranslation
            {
                FaqItemId = root.Id,
                LanguageCode = "en",
                Question = "When can I arrive?",
                AnswerHtmlSanitized = "<p>After 2 PM</p>",
                RowVersion = 13,
            };
            var child = new FaqItem
            {
                ResortId = resortId,
                CategoryId = category.Id,
                ParentId = root.Id,
                SortOrder = 2,
                IsActive = true,
                RowVersion = 17,
            };
            var childTranslation = new FaqItemTranslation
            {
                FaqItemId = child.Id,
                LanguageCode = "vi",
                Question = "Den som thi sao?",
                AnswerHtmlSanitized = "<p>Hay lien he le tan</p>",
                RowVersion = 19,
            };

            db.AddRange(category, categoryTranslation, root, rootTranslation, child, childTranslation);
            await db.SaveChangesAsync();
            categoryId = category.Id;
            categoryTranslationId = categoryTranslation.Id;
            rootId = root.Id;
            rootTranslationId = rootTranslation.Id;
            childId = child.Id;
            childTranslationId = childTranslation.Id;
        }

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUseCase<GetFaqAdminTreeInput, GetFaqAdminTreeResult>>();
        var result = await useCase.ExecuteAsync(new GetFaqAdminTreeInput(resortId));

        Assert.True(result.IsSuccess);
        Assert.Equal(EnabledLanguages, result.Value.EnabledLanguageCodes);
        Assert.Equal("en", result.Value.DefaultLanguageCode);

        var categoryResult = Assert.Single(result.Value.Categories);
        Assert.Equal(categoryId, categoryResult.CategoryId);
        Assert.Equal((uint)5, categoryResult.RowVersion);
        Assert.False(categoryResult.IsActive);
        Assert.Equal(MissingVietnameseAndKorean, categoryResult.MissingLanguages);
        var categoryTranslationResult = Assert.Single(categoryResult.Translations);
        Assert.Equal(categoryTranslationId, categoryTranslationResult.TranslationId);
        Assert.Equal((uint)7, categoryTranslationResult.RowVersion);
        Assert.Equal("Arrival", categoryTranslationResult.Name);

        var rootResult = Assert.Single(categoryResult.Items);
        Assert.Equal(rootId, rootResult.ItemId);
        Assert.Equal((uint)11, rootResult.RowVersion);
        Assert.False(rootResult.IsActive);
        Assert.Null(rootResult.ParentId);
        Assert.Equal(MissingVietnameseAndKorean, rootResult.MissingLanguages);
        var rootTranslationResult = Assert.Single(rootResult.Translations);
        Assert.Equal(rootTranslationId, rootTranslationResult.TranslationId);
        Assert.Equal((uint)13, rootTranslationResult.RowVersion);
        Assert.Equal("<p>After 2 PM</p>", rootTranslationResult.AnswerHtmlSanitized);

        var childResult = Assert.Single(rootResult.Children);
        Assert.Equal(childId, childResult.ItemId);
        Assert.Equal(rootId, childResult.ParentId);
        Assert.True(childResult.IsActive);
        Assert.Equal((uint)17, childResult.RowVersion);
        Assert.Equal(MissingEnglishAndKorean, childResult.MissingLanguages);
        var childTranslationResult = Assert.Single(childResult.Translations);
        Assert.Equal(childTranslationId, childTranslationResult.TranslationId);
        Assert.Equal((uint)19, childTranslationResult.RowVersion);
        Assert.Equal("vi", childTranslationResult.LanguageCode);
    }

    [Fact]
    public async Task Admin_tree_missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        config.Config = null;

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUseCase<GetFaqAdminTreeInput, GetFaqAdminTreeResult>>();
        var result = await useCase.ExecuteAsync(new GetFaqAdminTreeInput(Guid.CreateVersion7()));

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }
}
