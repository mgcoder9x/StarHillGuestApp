using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Rules.Application;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// CP13 (Req 3.7/3.8/3.9) — Acknowledge SERVER-AUTHORITATIVE + idempotent. SQLite in-memory (pre-check idempotency
/// provider-agnostic → chạy KHÔNG cần Docker; race qua unique constraint kiểm ở <c>RulesPostgresConstraintTests</c>).
/// Phủ: ack đầu ghi đúng publication IsCurrent + version + language; ack lặp cùng visit idempotent (không tạo trùng);
/// publish version mới → ack lại (bản mới); chưa publish → rules_unavailable; config null → configuration_unavailable;
/// ngôn ngữ chuẩn hóa về supported.
/// </summary>
public sealed class AcknowledgeRulesTests
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

    private sealed class StubGuestConfigQuery : IResortGuestConfigQuery
    {
        public ResortGuestConfig? Config { get; set; }
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(Config);
    }

    private static ResortGuestConfig ConfigFor(Guid resortId, params string[] langs) => new(
        resortId, "Star Hill", null, langs, langs[0],
        true, true, true, false, false, false, 30, 24);

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config)> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ITranslationResolver, TranslationResolver>();
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddRulesInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (provider, connection, config);
    }

    /// <summary>Seed một RulePublication; nếu <paramref name="makeCurrent"/> thì hạ mọi bản IsCurrent cũ của resort trước.</summary>
    private static async Task<Guid> SeedPublicationAsync(
        ServiceProvider provider, Guid resortId, int version, bool makeCurrent = true)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

        if (makeCurrent)
        {
            await foreach (var old in db.RulePublications.Where(p => p.ResortId == resortId && p.IsCurrent).AsAsyncEnumerable())
            {
                old.IsCurrent = false;
            }
        }

        var pub = new RulePublication
        {
            ResortId = resortId,
            Version = version,
            PublishedAt = DateTimeOffset.UnixEpoch,
            IsCurrent = makeCurrent,
        };
        db.RulePublications.Add(pub);
        await db.SaveChangesAsync();
        return pub.Id;
    }

    private static async Task<Bedrock.Domain.Results.Result<AcknowledgeRulesResult>> AckAsync(
        ServiceProvider provider, Guid resortId, Guid visitId, string? lang = "en", Guid? roomId = null, Guid? sessionId = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<AcknowledgeRulesInput, AcknowledgeRulesResult>>();
        return await uc.ExecuteAsync(new AcknowledgeRulesInput(
            resortId, roomId ?? Guid.CreateVersion7(), sessionId ?? Guid.CreateVersion7(), visitId, lang));
    }

    private static async Task<int> AckCountAsync(ServiceProvider provider, Guid visitId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        return await db.RuleAcknowledgements.CountAsync(a => a.GuestVisitId == visitId);
    }

    [Fact]
    public async Task First_acknowledge_records_current_publication()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi");
        var pubId = await SeedPublicationAsync(provider, resortId, version: 3);
        var visitId = Guid.CreateVersion7();

        var result = await AckAsync(provider, resortId, visitId, "vi");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.AlreadyAcknowledged);
        Assert.Equal(pubId, result.Value.RulePublicationId);
        Assert.Equal(3, result.Value.Version); // server dùng version của bản IsCurrent (CP13).

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        var ack = await db.RuleAcknowledgements.SingleAsync(a => a.GuestVisitId == visitId);
        Assert.Equal(pubId, ack.RulePublicationId);
        Assert.Equal(3, ack.Version);
        Assert.Equal("vi", ack.LanguageCode);
    }

    [Fact]
    public async Task Repeated_acknowledge_same_visit_is_idempotent()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");
        await SeedPublicationAsync(provider, resortId, version: 1);
        var visitId = Guid.CreateVersion7();

        var first = await AckAsync(provider, resortId, visitId);
        var second = await AckAsync(provider, resortId, visitId);

        Assert.False(first.Value.AlreadyAcknowledged);
        Assert.True(second.Value.AlreadyAcknowledged);
        Assert.Equal(1, await AckCountAsync(provider, visitId)); // KHÔNG tạo trùng.
    }

    [Fact]
    public async Task New_publication_requires_new_acknowledgement()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");
        await SeedPublicationAsync(provider, resortId, version: 1);
        var visitId = Guid.CreateVersion7();

        var ackV1 = await AckAsync(provider, resortId, visitId);
        Assert.False(ackV1.Value.AlreadyAcknowledged);

        // Publish bản mới (demote v1, v2 IsCurrent) → cùng visit phải ack lại.
        var pubV2 = await SeedPublicationAsync(provider, resortId, version: 2);
        var ackV2 = await AckAsync(provider, resortId, visitId);

        Assert.False(ackV2.Value.AlreadyAcknowledged); // bản khác → ack mới.
        Assert.Equal(pubV2, ackV2.Value.RulePublicationId);
        Assert.Equal(2, await AckCountAsync(provider, visitId)); // hai ack (v1 + v2) — lịch sử giữ.
    }

    [Fact]
    public async Task No_current_publication_returns_rules_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en");

        var result = await AckAsync(provider, resortId, Guid.CreateVersion7());

        Assert.False(result.IsSuccess);
        Assert.Equal("rules_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = null;
        await SeedPublicationAsync(provider, resortId, version: 1);

        var result = await AckAsync(provider, resortId, Guid.CreateVersion7());

        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Unknown_language_normalized_to_default()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigFor(resortId, "en", "vi"); // default en
        await SeedPublicationAsync(provider, resortId, version: 1);
        var visitId = Guid.CreateVersion7();

        await AckAsync(provider, resortId, visitId, "zz"); // không nằm trong enabled

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        var ack = await db.RuleAcknowledgements.SingleAsync(a => a.GuestVisitId == visitId);
        Assert.Equal("en", ack.LanguageCode); // chuẩn hóa về default supported.
    }
}
