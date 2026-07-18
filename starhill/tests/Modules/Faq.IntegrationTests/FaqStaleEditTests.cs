using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Faq.Application;
using Faq.Domain;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Faq.IntegrationTests;

public sealed class FaqStaleEditTests
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

    private sealed record SeedIds(Guid ResortId, Guid CategoryId, Guid ItemId);

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, SeedIds Ids)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddStarHillHtml();
        services.AddFaqInfrastructure(options => options.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        SeedIds ids;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            await db.Database.EnsureCreatedAsync();

            var resortId = Guid.CreateVersion7();
            var category = new FaqCategory
            {
                ResortId = resortId,
                Key = "arrival",
                SortOrder = 1,
                IsActive = true,
                RowVersion = 51,
            };
            var categoryTranslation = new FaqCategoryTranslation
            {
                FaqCategoryId = category.Id,
                LanguageCode = "en",
                Name = "Arrival",
                RowVersion = 53,
            };
            var item = new FaqItem
            {
                ResortId = resortId,
                CategoryId = category.Id,
                SortOrder = 1,
                IsActive = true,
                RowVersion = 55,
            };
            var itemTranslation = new FaqItemTranslation
            {
                FaqItemId = item.Id,
                LanguageCode = "en",
                Question = "When?",
                AnswerHtmlSanitized = "<p>After 2 PM</p>",
                RowVersion = 57,
            };
            db.AddRange(category, categoryTranslation, item, itemTranslation);
            await db.SaveChangesAsync();
            ids = new SeedIds(resortId, category.Id, item.Id);
        }

        return (provider, connection, ids);
    }

    [Fact]
    public async Task Category_update_and_delete_reject_stale_editor_version()
    {
        var (provider, connection, ids) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var update = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateFaqCategoryInput>>();
        var delete = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqCategoryInput>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => update.ExecuteAsync(
            new UpdateFaqCategoryInput(ids.CategoryId, 9, false, ExpectedRowVersion: 50)));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => delete.ExecuteAsync(
            new DeleteFaqCategoryInput(ids.CategoryId, ExpectedRowVersion: 50)));
    }

    [Fact]
    public async Task Item_update_and_delete_reject_stale_editor_version()
    {
        var (provider, connection, ids) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var update = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateFaqItemInput>>();
        var delete = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqItemInput>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => update.ExecuteAsync(
            new UpdateFaqItemInput(ids.ItemId, null, 9, false, ExpectedRowVersion: 54)));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => delete.ExecuteAsync(
            new DeleteFaqItemInput(ids.ItemId, ExpectedRowVersion: 54)));
    }

    [Fact]
    public async Task Translation_upserts_reject_stale_or_create_race_versions()
    {
        var (provider, connection, ids) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var categoryTranslation = scope.ServiceProvider.GetRequiredService<
            IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>>();
        var itemTranslation = scope.ServiceProvider.GetRequiredService<
            IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => categoryTranslation.ExecuteAsync(
            new UpsertFaqCategoryTranslationInput(ids.CategoryId, "en", "Changed", ExpectedRowVersion: null)));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => itemTranslation.ExecuteAsync(
            new UpsertFaqItemTranslationInput(ids.ItemId, "en", "Changed", "<p>Changed</p>", ExpectedRowVersion: 56)));
    }

    [Fact]
    public async Task Reorder_rejects_stale_versions_before_overwriting_sort_order()
    {
        var (provider, connection, ids) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var categories = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqCategoriesInput>>();
        var items = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqItemsInput>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => categories.ExecuteAsync(
            new ReorderFaqCategoriesInput(ids.ResortId, [new FaqReorderEntry(ids.CategoryId, 9, ExpectedRowVersion: 50)])));
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => items.ExecuteAsync(
            new ReorderFaqItemsInput(ids.CategoryId, [new FaqReorderEntry(ids.ItemId, 9, ExpectedRowVersion: 54)])));

        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
        Assert.Equal(1, (await db.FaqCategories.AsNoTracking().SingleAsync()).SortOrder);
        Assert.Equal(1, (await db.FaqItems.AsNoTracking().SingleAsync()).SortOrder);
    }
}
