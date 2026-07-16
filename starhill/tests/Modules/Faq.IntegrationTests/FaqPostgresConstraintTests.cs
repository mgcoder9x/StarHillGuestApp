using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Faq.Domain;
using Faq.Infrastructure.DependencyInjection;
using Faq.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Faq.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc DB module Faq — áp migration THẬT (đường production) để
/// chứng minh index/constraint có trong migration, không chỉ model. SKIP nếu thiếu Docker (N-067). Phủ: unique
/// <c>(ResortId, Key)</c> category; unique bản dịch <c>(category, lang)</c>/<c>(item, lang)</c> (Req 4/8.7);
/// FK Restrict chặn xóa category còn item (backstop DB cho invariant use case E-Faq.2). E-Faq.1 = persistence nền.
/// </summary>
public sealed class FaqPostgresConstraintTests : IAsyncLifetime
{
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

    private ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddFaqInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "faq")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
        await db.Database.MigrateAsync();
    }

    private static FaqCategory NewCategory(Guid resortId, string key) => new()
    {
        ResortId = resortId,
        Key = key,
        SortOrder = 1,
        IsActive = true,
    };

    [SkippableFact]
    public async Task Category_key_is_unique_per_resort()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();

        db.FaqCategories.Add(NewCategory(resortId, "arrival"));
        await db.SaveChangesAsync();

        // Cùng (ResortId, Key) → vi phạm ux_faq_category_key.
        db.FaqCategories.Add(NewCategory(resortId, "arrival"));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Category_translation_is_unique_per_category_and_language()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();

        var category = NewCategory(Guid.CreateVersion7(), "arrival");
        db.FaqCategories.Add(category);
        await db.SaveChangesAsync();

        db.FaqCategoryTranslations.Add(new FaqCategoryTranslation
        {
            FaqCategoryId = category.Id,
            LanguageCode = "en",
            Name = "Arrival",
        });
        await db.SaveChangesAsync();

        db.FaqCategoryTranslations.Add(new FaqCategoryTranslation
        {
            FaqCategoryId = category.Id,
            LanguageCode = "en", // cùng (category, lang) → vi phạm ux_faq_category_translation_lang.
            Name = "Dup",
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Item_translation_is_unique_per_item_and_language()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();

        var category = NewCategory(resortId, "arrival");
        db.FaqCategories.Add(category);
        var item = new FaqItem { ResortId = resortId, CategoryId = category.Id, SortOrder = 1, IsActive = true };
        db.FaqItems.Add(item);
        await db.SaveChangesAsync();

        db.FaqItemTranslations.Add(new FaqItemTranslation
        {
            FaqItemId = item.Id,
            LanguageCode = "en",
            Question = "What time is check-in?",
            AnswerHtmlSanitized = "<p>2 PM</p>",
        });
        await db.SaveChangesAsync();

        db.FaqItemTranslations.Add(new FaqItemTranslation
        {
            FaqItemId = item.Id,
            LanguageCode = "en", // cùng (item, lang) → vi phạm ux_faq_item_translation_lang.
            Question = "Dup",
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Cannot_delete_category_while_items_reference_it()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        // Seed category + item (scope riêng) rồi RỜI scope để không còn entity nào bị ChangeTracker theo dõi.
        Guid categoryId;
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var category = NewCategory(resortId, "arrival");
            db.FaqCategories.Add(category);
            db.FaqItems.Add(new FaqItem { ResortId = resortId, CategoryId = category.Id, SortOrder = 1, IsActive = true });
            await db.SaveChangesAsync();
            categoryId = category.Id;
        }

        // Xóa trong scope MỚI, chỉ load CHÍNH category (mirror DeleteFaqCategoryUseCase — KHÔNG track item con).
        // → lệnh DELETE chạm DB → FK Restrict (item→category, 23503) bắn → DbUpdateException (backstop invariant E-Faq.2).
        // (Nếu item con bị track cùng context, EF "sever" quan hệ client-side và ném InvalidOperationException TRƯỚC
        //  khi tới DB → không kiểm được ràng buộc DB thật. Đây là gốc rễ lỗi cũ, không phải nới lỏng ràng buộc.)
        await using (var deleteScope = provider.CreateAsyncScope())
        {
            var db = deleteScope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var category = await db.FaqCategories.SingleAsync(c => c.Id == categoryId);
            db.FaqCategories.Remove(category);
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
