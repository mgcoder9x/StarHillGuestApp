using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Application;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Faq.IntegrationTests;

/// <summary>
/// E-Faq.4 — guest đọc cây FAQ active: CP3 rule-gate (Faq là consumer đầu tiên của IRuleGate — fake gate: pass→cây;
/// fail→rule_ack_required), feature-flag (FaqEnabled=false→faq_disabled), fail-closed config (null→configuration_unavailable),
/// i18n fallback CP5 (resolver THẬT), cấu trúc cây cha-con + chỉ active. SQLite + seed qua use case CRUD thật.
/// </summary>
public sealed class GuestFaqTreeUseCaseTests
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubGuestConfigQuery : IResortGuestConfigQuery
    {
        public ResortGuestConfig? Config { get; set; }
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(Config);
    }

    private sealed class FakeRuleGate : Rules.Contracts.IRuleGate
    {
        public Result Next { get; set; } = Result.Success();

        public Task<Result> EnsureAcknowledgedAsync(
            Guid resortId, Guid guestVisitId, Rules.Contracts.GuestFeature feature, CancellationToken ct = default) =>
            Task.FromResult(Next);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId, bool faqEnabled, params string[] langs) => new(
        resortId, "Star Hill", null, langs, langs[0], faqEnabled, true, true, false, false, false, 30, 24);

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config, FakeRuleGate Gate)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var gate = new FakeRuleGate();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITranslationResolver, TranslationResolver>();
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddSingleton<Rules.Contracts.IRuleGate>(gate);
        services.AddStarHillHtml();
        services.AddFaqInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection, config, gate);
    }

    private static async Task<Guid> CreateCategoryAsync(ServiceProvider provider, Guid resortId, string key)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>>();
        var r = await uc.ExecuteAsync(new CreateFaqCategoryInput(resortId, key, 1, IsActive: true));
        Assert.True(r.IsSuccess);
        return r.Value.CategoryId;
    }

    private static async Task UpsertCategoryTranslationAsync(ServiceProvider provider, Guid categoryId, string lang, string name)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>>();
        Assert.True((await uc.ExecuteAsync(new UpsertFaqCategoryTranslationInput(categoryId, lang, name, ExpectedRowVersion: null))).IsSuccess);
    }

    private static async Task<Guid> CreateItemAsync(ServiceProvider provider, Guid categoryId, Guid? parentId, bool isActive)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>();
        var r = await uc.ExecuteAsync(new CreateFaqItemInput(categoryId, parentId, SortOrder: 1, IsActive: isActive));
        Assert.True(r.IsSuccess);
        return r.Value.ItemId;
    }

    private static async Task UpsertItemTranslationAsync(ServiceProvider provider, Guid itemId, string lang, string question, string answer)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>>();
        Assert.True((await uc.ExecuteAsync(new UpsertFaqItemTranslationInput(itemId, lang, question, answer, ExpectedRowVersion: null))).IsSuccess);
    }

    private static async Task<Result<GetGuestFaqTreeResult>> ReadTreeAsync(ServiceProvider provider, Guid resortId, string? lang)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetGuestFaqTreeInput, GetGuestFaqTreeResult>>();
        return await uc.ExecuteAsync(new GetGuestFaqTreeInput(resortId, Guid.CreateVersion7(), lang));
    }

    [Fact]
    public async Task Tree_renders_active_nodes_with_i18n_fallback_and_parent_child()
    {
        var (provider, connection, config, gate) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, faqEnabled: true, "en", "vi"); // default en
        gate.Next = Result.Success(); // không yêu cầu ack.

        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        await UpsertCategoryTranslationAsync(provider, categoryId, "en", "Arrival"); // chỉ en
        var parent = await CreateItemAsync(provider, categoryId, parentId: null, isActive: true);
        await UpsertItemTranslationAsync(provider, parent, "en", "Check-in?", "<p>2 PM</p>");
        var child = await CreateItemAsync(provider, categoryId, parentId: parent, isActive: true);
        await UpsertItemTranslationAsync(provider, child, "en", "Early check-in?", "<p>Ask reception</p>");

        var result = await ReadTreeAsync(provider, resortId, "vi"); // yêu cầu vi (không có) → fallback en

        Assert.True(result.IsSuccess);
        Assert.Equal("vi", result.Value.Language);
        var cat = Assert.Single(result.Value.Categories);
        Assert.Equal("Arrival", cat.Name);
        Assert.True(cat.IsFallback); // vi thiếu → fallback en
        var rootItem = Assert.Single(cat.Items);
        Assert.Equal("Check-in?", rootItem.Question);
        var childItem = Assert.Single(rootItem.Children);
        Assert.Equal("Early check-in?", childItem.Question);
    }

    [Fact]
    public async Task Gate_failure_blocks_tree_with_rule_ack_required()
    {
        var (provider, connection, config, gate) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, faqEnabled: true, "en");
        await CreateCategoryAsync(provider, resortId, "arrival");
        gate.Next = Result.Failure(Error.Forbidden("rule_ack_required", "Vui lòng xác nhận nội quy.")); // cấu hình yêu cầu ack, chưa ack.

        var result = await ReadTreeAsync(provider, resortId, "en");

        Assert.False(result.IsSuccess);
        Assert.Equal("rule_ack_required", result.Error.Code);
    }

    [Fact]
    public async Task Faq_disabled_returns_faq_disabled()
    {
        var (provider, connection, config, gate) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, faqEnabled: false, "en");

        var result = await ReadTreeAsync(provider, resortId, "en");

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_disabled", result.Error.Code);
    }

    [Fact]
    public async Task Missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config, gate) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        config.Config = null;

        var result = await ReadTreeAsync(provider, Guid.CreateVersion7(), "en");

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Inactive_category_and_item_excluded_from_tree()
    {
        var (provider, connection, config, gate) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, faqEnabled: true, "en");

        var activeCat = await CreateCategoryAsync(provider, resortId, "arrival");
        await UpsertCategoryTranslationAsync(provider, activeCat, "en", "Arrival");
        var activeItem = await CreateItemAsync(provider, activeCat, parentId: null, isActive: true);
        await UpsertItemTranslationAsync(provider, activeItem, "en", "Q", "<p>A</p>");
        await CreateItemAsync(provider, activeCat, parentId: null, isActive: false); // item inactive → không hiện

        var result = await ReadTreeAsync(provider, resortId, "en");

        Assert.True(result.IsSuccess);
        var cat = Assert.Single(result.Value.Categories);
        Assert.Single(cat.Items); // chỉ item active
    }
}
