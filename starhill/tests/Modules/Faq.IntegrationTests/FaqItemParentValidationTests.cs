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
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Faq.IntegrationTests;

/// <summary>
/// Bất biến CÂY (quyết định AI tự ra — spec chỉ nói "cha-con"): <c>ParentId</c> phải cùng danh mục, không tự trỏ,
/// không tạo CHU TRÌNH (chống render loop guest). Chứng minh qua use case Create/Update THẬT + SQLite (logic
/// provider-agnostic — chạy cục bộ). Vi phạm → <c>faq_invalid_parent</c>.
/// </summary>
public sealed class FaqItemParentValidationTests
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

    private static async Task<Result<CreateFaqItemResult>> CreateItemAsync(
        ServiceProvider provider, Guid categoryId, Guid? parentId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>();
        return await uc.ExecuteAsync(new CreateFaqItemInput(categoryId, parentId, SortOrder: 1, IsActive: true));
    }

    [Fact]
    public async Task Create_child_with_parent_in_same_category_succeeds()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");

        var parent = await CreateItemAsync(provider, categoryId, parentId: null);
        Assert.True(parent.IsSuccess);

        var child = await CreateItemAsync(provider, categoryId, parentId: parent.Value.ItemId);
        Assert.True(child.IsSuccess);
    }

    [Fact]
    public async Task Create_child_with_parent_in_different_category_fails()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var cat1 = await CreateCategoryAsync(provider, resortId, "arrival");
        var cat2 = await CreateCategoryAsync(provider, resortId, "dining");

        var itemInCat1 = await CreateItemAsync(provider, cat1, parentId: null);
        Assert.True(itemInCat1.IsSuccess);

        // Parent thuộc cat1 nhưng item mới ở cat2 → khác danh mục → invalid.
        var result = await CreateItemAsync(provider, cat2, parentId: itemInCat1.Value.ItemId);
        Assert.False(result.IsSuccess);
        Assert.Equal("faq_invalid_parent", result.Error.Code);
    }

    [Fact]
    public async Task Create_child_with_nonexistent_parent_fails()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");

        var result = await CreateItemAsync(provider, categoryId, parentId: Guid.CreateVersion7());
        Assert.False(result.IsSuccess);
        Assert.Equal("faq_invalid_parent", result.Error.Code);
    }

    [Fact]
    public async Task Update_item_to_be_its_own_parent_fails()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");
        var item = await CreateItemAsync(provider, categoryId, parentId: null);
        Assert.True(item.IsSuccess);

        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateFaqItemInput>>();
        var result = await uc.ExecuteAsync(
            new UpdateFaqItemInput(item.Value.ItemId, ParentId: item.Value.ItemId, SortOrder: 1, IsActive: true));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_invalid_parent", result.Error.Code);
    }

    [Fact]
    public async Task Update_item_to_create_cycle_fails()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId, "arrival");

        var a = await CreateItemAsync(provider, categoryId, parentId: null);
        Assert.True(a.IsSuccess);
        var b = await CreateItemAsync(provider, categoryId, parentId: a.Value.ItemId); // B con của A.
        Assert.True(b.IsSuccess);

        // Đặt A.parent = B → B là hậu duệ của A → tạo chu trình A→B→A → invalid.
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateFaqItemInput>>();
        var result = await uc.ExecuteAsync(
            new UpdateFaqItemInput(a.Value.ItemId, ParentId: b.Value.ItemId, SortOrder: 1, IsActive: true));

        Assert.False(result.IsSuccess);
        Assert.Equal("faq_invalid_parent", result.Error.Code);
    }
}
