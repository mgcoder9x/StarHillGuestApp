using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Application;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// CP12 (Req 8.6/11.4) — SANITIZE-ON-SAVE là bất biến chuẩn: nội dung nội quy do admin nhập đi qua adapter
/// <c>IHtmlSanitizer</c> THẬT (Ganss allowlist, đăng ký <c>AddStarHillHtml</c> — QR-AD-031) TRƯỚC khi lưu vào cột
/// <c>BodyHtmlSanitized</c>. Test này chứng minh đầu-cuối qua use case + DbContext THẬT: HTML độc (script/onerror/
/// javascript: uri) KHÔNG bao giờ nằm trong bản ghi lưu; đồng thời markup an toàn (&lt;p&gt;/&lt;strong&gt;) được GIỮ
/// (chứng minh là "làm sạch" chứ không "xóa trắng"). Sanitize áp CẢ Title lẫn Body (design §7 — chống XSS mọi bề mặt).
/// <para>
/// SQLite in-memory (quan hệ thật, CHẠY CỤC BỘ — sanitize là logic tầng Application provider-agnostic, không cần
/// Postgres). Concurrency (xmin) đo riêng ở <see cref="RuleConcurrencyTests"/> (buộc Npgsql).
/// </para>
/// </summary>
public sealed class RuleSanitizeTests
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
        services.AddStarHillHtml(); // adapter IHtmlSanitizer THẬT (Ganss) — như Host wiring D-Rules.4.
        services.AddRulesInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection);
    }

    private static async Task<Guid> CreateSectionAsync(ServiceProvider provider, Guid resortId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateRuleSectionInput, CreateRuleSectionResult>>();
        var result = await uc.ExecuteAsync(new CreateRuleSectionInput(resortId, "welcome", 1, IsRequired: true, RequireScrollEnd: false, MinReadSeconds: 0));
        Assert.True(result.IsSuccess);
        return result.Value.SectionId;
    }

    [Fact]
    public async Task Upsert_translation_strips_dangerous_html_but_keeps_safe_markup()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var sectionId = await CreateSectionAsync(provider, resortId);

        const string maliciousBody =
            "<p>Chào mừng <strong>quý khách</strong></p>" +
            "<script>alert('xss')</script>" +
            "<img src=x onerror=\"alert(1)\" />" +
            "<a href=\"javascript:alert(2)\">click</a>";
        const string maliciousTitle = "Nội quy<script>steal()</script>";

        Guid translationId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>();
            var result = await uc.ExecuteAsync(
                new UpsertRuleSectionTranslationInput(sectionId, "vi", maliciousTitle, maliciousBody, ExpectedRowVersion: null));
            Assert.True(result.IsSuccess);
            translationId = result.Value.TranslationId;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var stored = await db.RuleSectionTranslations.SingleAsync(t => t.Id == translationId);

            // (1) Không còn vector độc trong Body ĐÃ LƯU.
            Assert.NotNull(stored.BodyHtmlSanitized);
            Assert.DoesNotContain("<script", stored.BodyHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", stored.BodyHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", stored.BodyHtmlSanitized, StringComparison.OrdinalIgnoreCase);

            // (2) Markup AN TOÀN được giữ (làm sạch, không xóa trắng).
            Assert.Contains("<p>", stored.BodyHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<strong>", stored.BodyHtmlSanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("quý khách", stored.BodyHtmlSanitized, StringComparison.Ordinal);

            // (3) Title cũng được sanitize (chống XSS mọi bề mặt — design §7).
            Assert.NotNull(stored.Title);
            Assert.DoesNotContain("<script", stored.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Nội quy", stored.Title, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Upsert_translation_normalizes_blank_content_to_null()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var sectionId = await CreateSectionAsync(provider, resortId);

        Guid translationId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>();
            var result = await uc.ExecuteAsync(
                new UpsertRuleSectionTranslationInput(sectionId, "en", Title: "   ", BodyHtml: null, ExpectedRowVersion: null));
            Assert.True(result.IsSuccess);
            translationId = result.Value.TranslationId;
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var stored = await db.RuleSectionTranslations.SingleAsync(t => t.Id == translationId);
            Assert.Null(stored.Title);
            Assert.Null(stored.BodyHtmlSanitized);
        }
    }

    [Fact]
    public async Task Upsert_translation_is_idempotent_update_on_same_section_and_language()
    {
        var (provider, connection) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var sectionId = await CreateSectionAsync(provider, resortId);

        async Task<(Guid Id, uint RowVersion)> Upsert(string body, uint? expectedRowVersion)
        {
            await using var scope = provider.CreateAsyncScope();
            var uc = scope.ServiceProvider
                .GetRequiredService<IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>();
            var result = await uc.ExecuteAsync(
                new UpsertRuleSectionTranslationInput(sectionId, "vi", "T", body, expectedRowVersion));
            Assert.True(result.IsSuccess);
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var rowVersion = await db.RuleSectionTranslations
                .Where(translation => translation.Id == result.Value.TranslationId)
                .Select(translation => translation.RowVersion)
                .SingleAsync();
            return (result.Value.TranslationId, rowVersion);
        }

        var first = await Upsert("<p>một</p>", expectedRowVersion: null);
        var second = await Upsert("<p>hai</p>", first.RowVersion);

        // Cùng (section, lang) → UPSERT cập nhật đúng một bản ghi (không tạo trùng).
        Assert.Equal(first.Id, second.Id);
        await using var readScope = provider.CreateAsyncScope();
        var db = readScope.ServiceProvider.GetRequiredService<RulesDbContext>();
        var count = await db.RuleSectionTranslations.CountAsync(t => t.RuleSectionId == sectionId && t.LanguageCode == "vi");
        Assert.Equal(1, count);
        var stored = await db.RuleSectionTranslations.SingleAsync(t => t.Id == second.Id);
        Assert.Contains("hai", stored.BodyHtmlSanitized!, StringComparison.Ordinal);
    }
}
