using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Faq.Application;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Faq.IntegrationTests;

/// <summary>
/// E-Faq.3 (Req 4.5 drag-drop) — reorder cập nhật <c>SortOrder</c> hàng loạt NGUYÊN TỬ (một transaction), chỉ trong
/// đúng scope: danh mục theo resort, item theo category. Sai scope → not_found (chống sửa chéo resort/category).
/// SQLite (logic Application provider-agnostic). Validator (Id trùng/rỗng) test riêng ở <see cref="ReorderValidatorTests"/>.
/// </summary>
public sealed class ReorderFaqUseCaseTests
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

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddStarHillHtml();
        services.AddFaqInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateCategoryAsync(ServiceProvider provider, Guid resortId, string key, int sortOrder)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqCategoryInput(resortId, key, sortOrder, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.CategoryId;
    }

    private static async Task<Guid> CreateItemAsync(ServiceProvider provider, Guid categoryId, int sortOrder)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqItemInput(categoryId, ParentId: null, SortOrder: sortOrder, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.ItemId;
    }

    [Fact]
    public async Task Reorder_categories_updates_sort_order()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var a = await CreateCategoryAsync(provider, resortId, "arrival", 1);
        var b = await CreateCategoryAsync(provider, resortId, "dining", 2);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqCategoriesInput>>();
            var result = await uc.ExecuteAsync(new ReorderFaqCategoriesInput(
                resortId, [new FaqReorderEntry(a, 10, 0), new FaqReorderEntry(b, 5, 0)]));
            Assert.True(result.IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            Assert.Equal(10, (await db.FaqCategories.SingleAsync(c => c.Id == a)).SortOrder);
            Assert.Equal(5, (await db.FaqCategories.SingleAsync(c => c.Id == b)).SortOrder);
        }
    }

    [Fact]
    public async Task Reorder_categories_rejects_category_from_other_resort()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortA = Guid.CreateVersion7();
        var resortB = Guid.CreateVersion7();
        var catA = await CreateCategoryAsync(provider, resortA, "arrival", 1);
        var catB = await CreateCategoryAsync(provider, resortB, "arrival", 1);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqCategoriesInput>>();
        // catB thuộc resortB nhưng gửi kèm scope resortA → sai scope → not_found (chống sửa chéo resort).
        var result = await uc.ExecuteAsync(new ReorderFaqCategoriesInput(
            resortA, [new FaqReorderEntry(catA, 2, 0), new FaqReorderEntry(catB, 3, 0)]));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_category_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Reorder_items_updates_sort_order()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival", 1);
        var i1 = await CreateItemAsync(provider, categoryId, 1);
        var i2 = await CreateItemAsync(provider, categoryId, 2);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqItemsInput>>();
            var result = await uc.ExecuteAsync(new ReorderFaqItemsInput(
                categoryId, [new FaqReorderEntry(i1, 20, 0), new FaqReorderEntry(i2, 10, 0)]));
            Assert.True(result.IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            Assert.Equal(20, (await db.FaqItems.SingleAsync(i => i.Id == i1)).SortOrder);
            Assert.Equal(10, (await db.FaqItems.SingleAsync(i => i.Id == i2)).SortOrder);
        }
    }

    [Fact]
    public async Task Reorder_items_rejects_item_outside_category()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var cat1 = await CreateCategoryAsync(provider, resortId, "arrival", 1);
        var cat2 = await CreateCategoryAsync(provider, resortId, "dining", 2);
        var itemInCat1 = await CreateItemAsync(provider, cat1, 1);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<ReorderFaqItemsInput>>();
        // item thuộc cat1 nhưng gửi kèm scope cat2 → sai scope → not_found.
        var result = await uc.ExecuteAsync(new ReorderFaqItemsInput(
            cat2, [new FaqReorderEntry(itemInCat1, 5, 0)]));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_item_not_found", result.Error.Code);
    }
}

/// <summary>Unit test validator reorder (chạy độc lập adapter DI — chống Id trùng/danh sách rỗng trước khi vào use case).</summary>
public sealed class ReorderValidatorTests
{
    [Fact]
    public void Categories_validator_rejects_duplicate_ids()
    {
        var id = Guid.CreateVersion7();
        var result = new ReorderFaqCategoriesValidator().Validate(new ReorderFaqCategoriesInput(
            Guid.CreateVersion7(), [new FaqReorderEntry(id, 1, 0), new FaqReorderEntry(id, 2, 0)]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Categories_validator_rejects_empty_entries()
    {
        var result = new ReorderFaqCategoriesValidator().Validate(new ReorderFaqCategoriesInput(Guid.CreateVersion7(), []));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Items_validator_rejects_negative_sort_order()
    {
        var result = new ReorderFaqItemsValidator().Validate(new ReorderFaqItemsInput(
            Guid.CreateVersion7(), [new FaqReorderEntry(Guid.CreateVersion7(), -1, 0)]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Items_validator_accepts_valid_input()
    {
        var result = new ReorderFaqItemsValidator().Validate(new ReorderFaqItemsInput(
            Guid.CreateVersion7(), [new FaqReorderEntry(Guid.CreateVersion7(), 0, 0), new FaqReorderEntry(Guid.CreateVersion7(), 1, 0)]));
        Assert.True(result.IsValid);
    }
}
