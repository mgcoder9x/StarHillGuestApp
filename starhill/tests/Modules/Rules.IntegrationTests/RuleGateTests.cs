using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Application.Localization;
using ResortConfig.Contracts.Localization;
using ResortConfig.Contracts.Queries;
using Rules.Contracts;
using Rules.Domain;
using Rules.Infrastructure.DependencyInjection;
using Rules.Infrastructure.Persistence;
using Xunit;

namespace Rules.IntegrationTests;

/// <summary>
/// CP3 (Req 3.11/14) — rule-gate BACKEND server-authoritative. SQLite in-memory (logic gate provider-agnostic →
/// KHÔNG cần Docker). Phủ: cờ tắt→cho qua (dù chưa ack); cờ bật+chưa publish→chặn; cờ bật+chưa ack→chặn (403
/// rule_ack_required); cờ bật+đã ack→qua; tính năng độc lập theo cờ riêng; config null→configuration_unavailable.
/// </summary>
public sealed class RuleGateTests
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

    private static ResortGuestConfig ConfigWith(Guid resortId, bool faqAck, bool chatAck, bool hkAck) => new(
        resortId, "Star Hill", null, ["en"], "en",
        true, true, true, faqAck, chatAck, hkAck, 30, 24);

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

    private static async Task<Guid> SeedPublicationAsync(ServiceProvider provider, Guid resortId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        var pub = new RulePublication { ResortId = resortId, Version = 1, PublishedAt = DateTimeOffset.UnixEpoch, IsCurrent = true };
        db.RulePublications.Add(pub);
        await db.SaveChangesAsync();
        return pub.Id;
    }

    private static async Task SeedAckAsync(ServiceProvider provider, Guid resortId, Guid visitId, Guid pubId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
        db.RuleAcknowledgements.Add(new RuleAcknowledgement
        {
            ResortId = resortId,
            RoomId = Guid.CreateVersion7(),
            GuestSessionId = Guid.CreateVersion7(),
            GuestVisitId = visitId,
            RulePublicationId = pubId,
            Version = 1,
            LanguageCode = "en",
            AcceptedAt = DateTimeOffset.UnixEpoch,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Bedrock.Domain.Results.Result> GateAsync(
        ServiceProvider provider, Guid resortId, Guid visitId, GuestFeature feature)
    {
        await using var scope = provider.CreateAsyncScope();
        var gate = scope.ServiceProvider.GetRequiredService<IRuleGate>();
        return await gate.EnsureAcknowledgedAsync(resortId, visitId, feature);
    }

    [Fact]
    public async Task Flag_off_allows_without_acknowledgement()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigWith(resortId, faqAck: false, chatAck: false, hkAck: false);
        await SeedPublicationAsync(provider, resortId);

        var result = await GateAsync(provider, resortId, Guid.CreateVersion7(), GuestFeature.Faq);

        Assert.True(result.IsSuccess); // cờ tắt → cho qua dù chưa ack.
    }

    [Fact]
    public async Task Flag_on_without_acknowledgement_is_forbidden()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigWith(resortId, faqAck: true, chatAck: false, hkAck: false);
        await SeedPublicationAsync(provider, resortId);

        var result = await GateAsync(provider, resortId, Guid.CreateVersion7(), GuestFeature.Faq);

        Assert.True(result.IsFailure);
        Assert.Equal("rule_ack_required", result.Error.Code);
    }

    [Fact]
    public async Task Flag_on_with_acknowledgement_allows()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        config.Config = ConfigWith(resortId, faqAck: true, chatAck: false, hkAck: false);
        var pubId = await SeedPublicationAsync(provider, resortId);
        await SeedAckAsync(provider, resortId, visitId, pubId);

        var result = await GateAsync(provider, resortId, visitId, GuestFeature.Faq);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Flag_on_without_publication_is_forbidden()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        config.Config = ConfigWith(resortId, faqAck: true, chatAck: false, hkAck: false);
        // KHÔNG seed publication.

        var result = await GateAsync(provider, resortId, Guid.CreateVersion7(), GuestFeature.Faq);

        Assert.True(result.IsFailure);
        Assert.Equal("rule_ack_required", result.Error.Code);
    }

    [Fact]
    public async Task Feature_flags_are_independent()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        config.Config = ConfigWith(resortId, faqAck: true, chatAck: false, hkAck: false); // chỉ Faq gate.
        await SeedPublicationAsync(provider, resortId);

        Assert.True((await GateAsync(provider, resortId, visitId, GuestFeature.Chat)).IsSuccess);        // cờ tắt.
        Assert.True((await GateAsync(provider, resortId, visitId, GuestFeature.Housekeeping)).IsSuccess); // cờ tắt.
        Assert.True((await GateAsync(provider, resortId, visitId, GuestFeature.Faq)).IsFailure);          // cờ bật + chưa ack.
    }

    [Fact]
    public async Task Missing_config_returns_configuration_unavailable()
    {
        var (provider, connection, config) = await BuildAsync();
        await using var _ = provider;
        await using var __ = connection;

        config.Config = null;

        var result = await GateAsync(provider, Guid.CreateVersion7(), Guid.CreateVersion7(), GuestFeature.Faq);

        Assert.True(result.IsFailure);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }
}
