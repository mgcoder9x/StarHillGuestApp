using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Application;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// CP4 (Req 8) — PUBLISH tạo snapshot bất biến: khách đọc từ <c>RulePublication</c> IsCurrent; sửa Draft sau publish
/// KHÔNG đổi snapshot đã publish; đúng MỘT publication IsCurrent/resort (flip-before-insert nguyên tử — QR-AD-030/036).
/// Chạy trên PostgreSQL THẬT qua migration (đường production) để chứng minh partial-unique <c>ux_rule_publication_current</c>
/// + thứ tự hạ-cũ-trước-insert-mới hoạt động (SQLite không phản ánh trung thực partial-unique filter Npgsql). SKIP nếu
/// thiếu Docker (N-067). Draft seed trực tiếp qua DbContext (nội dung đã sạch — sanitize đo riêng ở RuleSanitizeTests).
/// </summary>
public sealed class PublishRulesUseCaseTests : IAsyncLifetime
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
        services.AddRulesInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "rules")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Seed Draft: RuleSet + 2 section (sortorder 2,1 để kiểm sắp xếp) + 1 translation "welcome".</summary>
    private static async Task SeedDraftAsync(ServiceProvider provider, Guid resortId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var set = new RuleSet { ResortId = resortId, UpdatedAt = DateTimeOffset.UnixEpoch };
        db.RuleSets.Add(set);

        var s1 = new RuleSection { RuleSetId = set.Id, Key = "checkout", SortOrder = 2, IsRequired = true, MinReadSeconds = 5 };
        var s2 = new RuleSection { RuleSetId = set.Id, Key = "welcome", SortOrder = 1, IsRequired = false, RequireScrollEnd = true };
        db.RuleSections.AddRange(s1, s2);

        db.RuleSectionTranslations.Add(new RuleSectionTranslation
        {
            RuleSectionId = s2.Id,
            LanguageCode = "en",
            Title = "Welcome",
            BodyHtmlSanitized = "<p>Hello</p>",
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Result<PublishRulesResult>> PublishAsync(ServiceProvider provider, Guid resortId, string? note = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<PublishRulesInput, PublishRulesResult>>();
        return await uc.ExecuteAsync(new PublishRulesInput(resortId, PublishedByUserId: null, ChangeNote: note));
    }

    [SkippableFact]
    public async Task First_publish_creates_current_version_1_with_frozen_ordered_sections()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        await SeedDraftAsync(provider, resortId);

        var result = await PublishAsync(provider, resortId, note: "first");
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Version);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var pub = await db.RulePublications.AsNoTracking().SingleAsync(p => p.ResortId == resortId);
        Assert.True(pub.IsCurrent);
        Assert.Equal(1, pub.Version);
        Assert.Equal("first", pub.ChangeNote);
        Assert.Equal(result.Value.PublicationId, pub.Id);

        var sections = await db.RulePublicationSections.AsNoTracking()
            .Where(s => s.RulePublicationId == pub.Id)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        Assert.Equal(2, sections.Count);
        Assert.Equal("welcome", sections[0].Key);  // SortOrder 1 trước
        Assert.Equal("checkout", sections[1].Key);  // SortOrder 2 sau
        Assert.True(sections[0].RequireScrollEnd);
        Assert.Equal(5, sections[1].MinReadSeconds);

        var welcomeTr = await db.RulePublicationSectionTranslations.AsNoTracking()
            .SingleAsync(t => t.RulePublicationSectionId == sections[0].Id);
        Assert.Equal("en", welcomeTr.LanguageCode);
        Assert.Equal("Welcome", welcomeTr.Title);
        Assert.Equal("<p>Hello</p>", welcomeTr.BodyHtmlSanitized);
    }

    [SkippableFact]
    public async Task Second_publish_demotes_previous_and_keeps_single_current()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        await SeedDraftAsync(provider, resortId);

        var first = await PublishAsync(provider, resortId, "v1");
        var second = await PublishAsync(provider, resortId, "v2");
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, first.Value.Version);
        Assert.Equal(2, second.Value.Version);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var all = await db.RulePublications.AsNoTracking().Where(p => p.ResortId == resortId).ToListAsync();
        Assert.Equal(2, all.Count);
        Assert.Single(all, p => p.IsCurrent); // ĐÚNG MỘT current (flip-before-insert giữ partial-unique)
        var current = all.Single(p => p.IsCurrent);
        Assert.Equal(2, current.Version);
        Assert.Equal(second.Value.PublicationId, current.Id);
        Assert.False(all.Single(p => p.Version == 1).IsCurrent);
    }

    [SkippableFact]
    public async Task Editing_draft_after_publish_does_not_change_published_snapshot()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        await SeedDraftAsync(provider, resortId);

        var published = await PublishAsync(provider, resortId, "v1");
        Assert.True(published.IsSuccess);

        // Sửa Draft SAU publish: đổi body translation + thêm section mới.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var tr = await db.RuleSectionTranslations.SingleAsync(t => t.LanguageCode == "en");
            tr.BodyHtmlSanitized = "<p>CHANGED</p>";
            var set = await db.RuleSets.SingleAsync(rs => rs.ResortId == resortId);
            db.RuleSections.Add(new RuleSection { RuleSetId = set.Id, Key = "extra", SortOrder = 3 });
            await db.SaveChangesAsync();
        }

        // Snapshot đã publish KHÔNG đổi (CP4): vẫn 2 section, body vẫn "<p>Hello</p>".
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var pub = await db.RulePublications.AsNoTracking().SingleAsync(p => p.Id == published.Value.PublicationId);
            var sectionCount = await db.RulePublicationSections.AsNoTracking().CountAsync(s => s.RulePublicationId == pub.Id);
            Assert.Equal(2, sectionCount);
            var bodies = await db.RulePublicationSectionTranslations.AsNoTracking()
                .Where(t => t.LanguageCode == "en")
                .Select(t => t.BodyHtmlSanitized)
                .ToListAsync();
            Assert.Contains("<p>Hello</p>", bodies);
            Assert.DoesNotContain("<p>CHANGED</p>", bodies);
        }
    }

    [SkippableFact]
    public async Task Publish_with_no_draft_fails_with_no_publishable_content()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var result = await PublishAsync(provider, Guid.CreateVersion7());
        Assert.False(result.IsSuccess);
        Assert.Equal("validation_error", result.Error.Code);
    }
}
