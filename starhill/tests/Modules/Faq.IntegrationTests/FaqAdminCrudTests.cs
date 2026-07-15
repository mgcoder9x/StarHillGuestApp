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
/// E-Faq.2 — invariant toàn vẹn cây ở tầng USE CASE (không chỉ FK Restrict DB): xóa danh mục còn item →
/// <c>faq_category_not_empty</c>; xóa mục còn con → <c>faq_item_has_children</c>; leaf/danh mục rỗng xóa được.
/// Happy-path CRUD (tạo category/item + đọc lại). SQLite (logic Application provider-agnostic — chạy cục bộ).
/// </summary>
public sealed class FaqAdminCrudTests
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

    private static async Task<Guid> CreateCategoryAsync(ServiceProvider provider, Guid resortId, string key)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqCategoryInput(resortId, key, 1, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.CategoryId;
    }

    private static async Task<Guid> CreateItemAsync(ServiceProvider provider, Guid categoryId, Guid? parentId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqItemInput(categoryId, parentId, SortOrder: 1, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.ItemId;
    }

    [Fact]
    public async Task Create_item_derives_resort_from_category()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        var itemId = await CreateItemAsync(provider, categoryId, parentId: null);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
        var item = await db.FaqItems.SingleAsync(i => i.Id == itemId);
        Assert.Equal(resortId, item.ResortId); // ResortId DERIVE từ category (không trust input).
        Assert.Equal(categoryId, item.CategoryId);
    }

    [Fact]
    public async Task Delete_category_with_items_is_blocked()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        await CreateItemAsync(provider, categoryId, parentId: null);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqCategoryInput>>();
        var result = await uc.ExecuteAsync(new DeleteFaqCategoryInput(categoryId));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_category_not_empty", result.Error.Code);
    }

    [Fact]
    public async Task Delete_item_with_children_is_blocked()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        var parentId = await CreateItemAsync(provider, categoryId, parentId: null);
        await CreateItemAsync(provider, categoryId, parentId: parentId);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqItemInput>>();
        var result = await uc.ExecuteAsync(new DeleteFaqItemInput(parentId));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_item_has_children", result.Error.Code);
    }

    [Fact]
    public async Task Delete_leaf_item_then_empty_category_succeeds()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        var itemId = await CreateItemAsync(provider, categoryId, parentId: null);

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqItemInput>>();
            Assert.True((await uc.ExecuteAsync(new DeleteFaqItemInput(itemId))).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteFaqCategoryInput>>();
            Assert.True((await uc.ExecuteAsync(new DeleteFaqCategoryInput(categoryId))).IsSuccess);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            Assert.Equal(0, await db.FaqItems.CountAsync());
            Assert.Equal(0, await db.FaqCategories.CountAsync());
        }
    }

    [Fact]
    public async Task Update_category_not_found_returns_error()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateFaqCategoryInput>>();
        var result = await uc.ExecuteAsync(new UpdateFaqCategoryInput(Guid.CreateVersion7(), SortOrder: 2, IsActive: false));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_category_not_found", result.Error.Code);
    }
}
