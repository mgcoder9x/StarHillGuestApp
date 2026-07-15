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
/// CP12 (Req 8.6/11.4) — SANITIZE-ON-SAVE cho FAQ: <c>Question</c>/<c>Answer</c> (item) và <c>Name</c> (category) do
/// admin nhập đi qua adapter <c>IHtmlSanitizer</c> THẬT (Ganss allowlist, <c>AddStarHillHtml</c> — QR-AD-031) TRƯỚC
/// khi lưu. Chứng minh đầu-cuối qua use case + DbContext THẬT: HTML độc (script/onerror/javascript:) KHÔNG nằm trong
/// bản ghi lưu; markup an toàn (&lt;p&gt;/&lt;strong&gt;) được GIỮ (làm sạch, không xóa trắng). SQLite in-memory
/// (sanitize là logic Application provider-agnostic — chạy cục bộ). Concurrency (xmin) đo riêng ở FaqConcurrencyTests.
/// </summary>
public sealed class FaqSanitizeTests
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
        services.AddStarHillHtml(); // adapter IHtmlSanitizer THẬT (Ganss) — như Host wiring.
        services.AddFaqInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateCategoryAsync(ServiceProvider provider, Guid resortId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqCategoryInput, CreateFaqCategoryResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqCategoryInput(resortId, "arrival", 1, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.CategoryId;
    }

    private static async Task<Guid> CreateItemAsync(ServiceProvider provider, Guid categoryId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateFaqItemInput, CreateFaqItemResult>>();
        var result = await uc.ExecuteAsync(new CreateFaqItemInput(categoryId, ParentId: null, SortOrder: 1, IsActive: true));
        Assert.True(result.IsSuccess);
        return result.Value.ItemId;
    }

    [Fact]
    public async Task Upsert_item_translation_strips_dangerous_html_but_keeps_safe_markup()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId);
        var itemId = await CreateItemAsync(provider, categoryId);

        const string maliciousAnswer =
            "<p>Nhận phòng lúc <strong>14:00</strong></p>" +
            "<script>alert('xss')</script>" +
            "<img src=x onerror=\"alert(1)\" />" +
            "<a href=\"javascript:alert(2)\">click</a>";
        const string maliciousQuestion = "Giờ nhận phòng?<script>steal()</script>";

        Guid translationId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>>();
            var result = await uc.ExecuteAsync(
                new UpsertFaqItemTranslationInput(itemId, "vi", maliciousQuestion, maliciousAnswer));
            Assert.True(result.IsSuccess);
            translationId = result.Value.TranslationId;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var stored = await db.FaqItemTranslations.SingleAsync(t => t.Id == translationId);

            Assert.NotNull(stored.AnswerHtmlSanitized);
            Assert.DoesNotContain("<script", stored.AnswerHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", stored.AnswerHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", stored.AnswerHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<p>", stored.AnswerHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<strong>", stored.AnswerHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("14:00", stored.AnswerHtmlSanitized, StringComparison.Ordinal);

            Assert.NotNull(stored.Question);
            Assert.DoesNotContain("<script", stored.Question, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Giờ nhận phòng", stored.Question, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Upsert_category_translation_sanitizes_name()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId);

        Guid translationId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertFaqCategoryTranslationInput, UpsertFaqCategoryTranslationResult>>();
            var result = await uc.ExecuteAsync(
                new UpsertFaqCategoryTranslationInput(categoryId, "vi", "Nhận phòng<script>x()</script>"));
            Assert.True(result.IsSuccess);
            translationId = result.Value.TranslationId;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FaqDbContext>();
            var stored = await db.FaqCategoryTranslations.SingleAsync(t => t.Id == translationId);
            Assert.NotNull(stored.Name);
            Assert.DoesNotContain("<script", stored.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Nhận phòng", stored.Name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Upsert_item_translation_normalizes_blank_to_null_and_is_idempotent()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var categoryId = await CreateCategoryAsync(provider, resortId);
        var itemId = await CreateItemAsync(provider, categoryId);

        async Task<Guid> Upsert(string? question, string? answer)
        {
            await using var scope = provider.CreateAsyncScope();
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertFaqItemTranslationInput, UpsertFaqItemTranslationResult>>();
            var result = await uc.ExecuteAsync(new UpsertFaqItemTranslationInput(itemId, "en", question, answer));
            Assert.True(result.IsSuccess);
            return result.Value.TranslationId;
        }

        var first = await Upsert("   ", null);
        var second = await Upsert("What time?", "<p>2 PM</p>");

        Assert.Equal(first, second); // upsert cùng (item, lang) → cập nhật, không tạo trùng.

        await using var readScope = provider.CreateAsyncScope();
        var db = readScope.ServiceProvider.GetRequiredService<FaqDbContext>();
        var count = await db.FaqItemTranslations.CountAsync(t => t.FaqItemId == itemId && t.LanguageCode == "en");
        Assert.Equal(1, count);
        var stored = await db.FaqItemTranslations.SingleAsync(t => t.Id == second);
        Assert.Contains("2 PM", stored.AnswerHtmlSanitized!, StringComparison.Ordinal);
    }
}
