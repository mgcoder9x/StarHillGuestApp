using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc DB module Rules — áp migration THẬT (đường production) để
/// chứng minh index/constraint có trong migration, không chỉ model. SKIP nếu thiếu Docker (N-067). Phủ: đúng MỘT
/// RulePublication IsCurrent/resort (CP4); ack idempotent unique (visit, publication) (Req 3.8); unique bản dịch
/// (section, lang) (Req 8.7). D-Rules.1 = persistence nền.
/// </summary>
public sealed class RulesPostgresConstraintTests : IAsyncLifetime
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

    private static RulePublication NewPublication(Guid resortId, int version, bool isCurrent) => new()
    {
        ResortId = resortId,
        Version = version,
        PublishedAt = DateTimeOffset.UnixEpoch,
        IsCurrent = isCurrent,
    };

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        await db.Database.MigrateAsync();
    }

    [SkippableFact]
    public async Task Only_one_current_publication_per_resort_but_reusable_after_demote()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        // (1) Hai publication IsCurrent cùng resort → vi phạm ux_rule_publication_current.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            db.RulePublications.Add(NewPublication(resortId, 1, isCurrent: true));
            await db.SaveChangesAsync();

            db.RulePublications.Add(NewPublication(resortId, 2, isCurrent: true));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // (2) Hạ bản cũ IsCurrent=false rồi thêm bản mới IsCurrent=true → CHO PHÉP (partial filter is_current).
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var current = await db.RulePublications.FirstAsync(p => p.IsCurrent);
            current.IsCurrent = false;
            await db.SaveChangesAsync();

            db.RulePublications.Add(NewPublication(resortId, 2, isCurrent: true));
            await db.SaveChangesAsync(); // không ném — slot current đã giải phóng (flip-before-insert — QR-AD-030).
        }
    }

    [SkippableFact]
    public async Task Acknowledgement_is_unique_per_visit_and_publication()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var publication = NewPublication(resortId, 1, isCurrent: true);
        db.RulePublications.Add(publication);
        await db.SaveChangesAsync();

        RuleAcknowledgement Ack() => new()
        {
            ResortId = resortId,
            RoomId = Guid.CreateVersion7(),
            GuestSessionId = Guid.CreateVersion7(),
            GuestVisitId = visitId,
            RulePublicationId = publication.Id,
            Version = 1,
            LanguageCode = "en",
            AcceptedAt = DateTimeOffset.UnixEpoch,
        };

        db.RuleAcknowledgements.Add(Ack());
        await db.SaveChangesAsync();

        db.RuleAcknowledgements.Add(Ack()); // cùng (visit, publication) → vi phạm ux_rule_ack_visit_publication.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Section_translation_is_unique_per_section_and_language()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        var set = new RuleSet { ResortId = Guid.CreateVersion7(), UpdatedAt = DateTimeOffset.UnixEpoch };
        db.RuleSets.Add(set);
        var section = new RuleSection { RuleSetId = set.Id, Key = "welcome", SortOrder = 1 };
        db.RuleSections.Add(section);
        await db.SaveChangesAsync();

        db.RuleSectionTranslations.Add(new RuleSectionTranslation
        {
            RuleSectionId = section.Id,
            LanguageCode = "en",
            Title = "Welcome",
            BodyHtmlSanitized = "<p>hi</p>",
        });
        await db.SaveChangesAsync();

        db.RuleSectionTranslations.Add(new RuleSectionTranslation
        {
            RuleSectionId = section.Id,
            LanguageCode = "en", // cùng (section, lang) → vi phạm ux_rule_section_translation_lang.
            Title = "Dup",
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Only_one_rule_set_per_resort(/* QR-AD-035 */)
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        db.RuleSets.Add(new RuleSet { ResortId = resortId, UpdatedAt = DateTimeOffset.UnixEpoch });
        await db.SaveChangesAsync();

        // Bản Draft thứ hai cùng resort → vi phạm ux_rule_set_resort (đúng-một-Draft/resort — QR-AD-035).
        // Đây là ràng buộc DB làm find-or-create của CreateRuleSectionUseCase race-safe (không TOCTOU).
        db.RuleSets.Add(new RuleSet { ResortId = resortId, UpdatedAt = DateTimeOffset.UnixEpoch });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
