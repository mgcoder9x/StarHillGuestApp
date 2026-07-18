using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rules.Application;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using StarHill.Html.DependencyInjection;
using Xunit;

namespace Rules.IntegrationTests;

public sealed class RuleStaleEditTests
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

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, Guid SectionId)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddStarHillHtml();
        services.AddRulesInfrastructure(options => options.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Guid sectionId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            await db.Database.EnsureCreatedAsync();

            var ruleSet = new RuleSet
            {
                ResortId = Guid.CreateVersion7(),
                UpdatedAt = DateTimeOffset.UnixEpoch,
                RowVersion = 31,
            };
            var section = new RuleSection
            {
                RuleSetId = ruleSet.Id,
                Key = "arrival",
                SortOrder = 1,
                MinReadSeconds = 10,
                RowVersion = 41,
            };
            var translation = new RuleSectionTranslation
            {
                RuleSectionId = section.Id,
                LanguageCode = "en",
                Title = "Arrival",
                BodyHtmlSanitized = "<p>Welcome</p>",
                RowVersion = 43,
            };
            db.AddRange(ruleSet, section, translation);
            await db.SaveChangesAsync();
            sectionId = section.Id;
        }

        return (provider, connection, sectionId);
    }

    [Fact]
    public async Task Section_update_rejects_stale_editor_version()
    {
        var (provider, connection, sectionId) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateRuleSectionInput>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => useCase.ExecuteAsync(new UpdateRuleSectionInput(
            sectionId, 9, true, true, 60, ExpectedRowVersion: 40)));

        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        Assert.Equal(10, (await db.RuleSections.AsNoTracking().SingleAsync()).MinReadSeconds);
    }

    [Fact]
    public async Task Section_delete_rejects_stale_editor_version()
    {
        var (provider, connection, sectionId) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteRuleSectionInput>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            useCase.ExecuteAsync(new DeleteRuleSectionInput(sectionId, ExpectedRowVersion: 40)));

        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        Assert.Equal(1, await db.RuleSections.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Translation_upsert_rejects_create_race_when_editor_expected_no_row()
    {
        var (provider, connection, sectionId) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<
            IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => useCase.ExecuteAsync(
            new UpsertRuleSectionTranslationInput(sectionId, "en", "Changed", "<p>Changed</p>", ExpectedRowVersion: null)));
    }

    [Fact]
    public async Task Translation_upsert_rejects_recreate_when_editor_expected_deleted_row()
    {
        var (provider, connection, sectionId) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        await using var scope = provider.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<
            IUseCase<UpsertRuleSectionTranslationInput, UpsertRuleSectionTranslationResult>>();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => useCase.ExecuteAsync(
            new UpsertRuleSectionTranslationInput(sectionId, "vi", "Moi", "<p>Moi</p>", ExpectedRowVersion: 47)));
    }
}
