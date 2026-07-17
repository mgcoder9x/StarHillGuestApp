using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Application;
using Concierge.Domain;
using Concierge.Infrastructure.DependencyInjection;
using Concierge.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResortConfig.Contracts.Queries;
using Xunit;

namespace Concierge.IntegrationTests;

/// <summary>
/// K-Con.2b — use case NHÂN VIÊN Concierge (SQLite + fake settings + spy notifier). Phủ reply (not-found/config-null/
/// empty/too-long/happy-append-staff-không-tăng-unread/reopen-Closed/notify) + mark-read (not-found/reset-unread+đánh-dấu-tin-guest+notify)
/// + close (not-found/happy+idempotent/notify) + close-for-visit (no-conversation-noop/đã-Closed-noop/System-ClosedByUserId-null).
/// Logic provider-agnostic → SQLite chạy cục bộ; board ordering (LastMessageAt) đo riêng Postgres (ConciergeBoardTests).
/// </summary>
public sealed class ConciergeStaffUseCaseTests
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
        public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class StubGuestConfigQuery : IResortGuestConfigQuery
    {
        public ResortGuestConfig? Config { get; set; }
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult(Config);
    }

    private sealed class StubSettingsQuery : IResortSettingsQuery
    {
        public ResortSettingsSnapshot? Snapshot { get; set; }
        public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) => Task.FromResult(Snapshot);
    }

    private sealed class FakeRuleGate : Rules.Contracts.IRuleGate
    {
        public Result Next { get; set; } = Result.Success();
        public Task<Result> EnsureAcknowledgedAsync(Guid resortId, Guid guestVisitId, Rules.Contracts.GuestFeature feature, CancellationToken ct = default) =>
            Task.FromResult(Next);
    }

    private sealed class SpyNotifier : IConciergeRealtimeNotifier
    {
        public int ReceivedCount { get; private set; }
        public int UpdatedCount { get; private set; }
        public int ReadCount { get; private set; }
        public Task NotifyMessageReceivedAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct = default)
        {
            ReceivedCount++;
            return Task.CompletedTask;
        }
        public Task NotifyConversationUpdatedAsync(Guid resortId, Guid conversationId, CancellationToken ct = default)
        {
            UpdatedCount++;
            return Task.CompletedTask;
        }
        public Task NotifyMessageReadAsync(Guid resortId, Guid conversationId, CancellationToken ct = default)
        {
            ReadCount++;
            return Task.CompletedTask;
        }
    }

    private static ResortSettingsSnapshot SettingsFor(Guid resortId, int maxMessageLength) => new(
        resortId, true, true, true, false, false, false, 30, 24, null, maxMessageLength, 10, 12);

    private sealed record Harness(
        ServiceProvider Provider, SqliteConnection Connection, StubSettingsQuery Settings, SpyNotifier Notifier);

    private static async Task<Harness> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var settings = new StubSettingsQuery();
        var notifier = new SpyNotifier();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<IResortGuestConfigQuery>(new StubGuestConfigQuery());
        services.AddSingleton<IResortSettingsQuery>(settings);
        services.AddSingleton<Rules.Contracts.IRuleGate>(new FakeRuleGate());
        services.AddConciergeInfrastructure(o => o.UseSqlite(connection));
        services.AddSingleton<IConciergeRealtimeNotifier>(notifier);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new Harness(provider, connection, settings, notifier);
    }

    /// <summary>Seed một hội thoại Open + N tin của KHÁCH (chưa đọc), trả conversationId.</summary>
    private static async Task<Guid> SeedConversationAsync(
        ServiceProvider provider, Guid resortId, Guid visitId, int guestMessages = 1, ConversationStatus status = ConversationStatus.Open)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var now = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var conversation = new Conversation
        {
            ResortId = resortId,
            RoomId = Guid.CreateVersion7(),
            GuestSessionId = Guid.CreateVersion7(),
            GuestVisitId = visitId,
            Status = status,
            LastMessageAt = now,
            LastGuestMessageAt = now,
            UnreadForStaff = guestMessages,
            CreatedAt = now,
            ClosedAt = status == ConversationStatus.Closed ? now : null,
            ClosedByUserId = status == ConversationStatus.Closed ? Guid.CreateVersion7() : null,
        };
        db.Conversations.Add(conversation);
        for (var i = 0; i < guestMessages; i++)
        {
            db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderType = MessageSenderType.Guest,
                Body = $"tin khach {i}",
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync();
        return conversation.Id;
    }

    private static async Task<Result<ReplyConversationResult>> ReplyAsync(
        ServiceProvider provider, Guid conversationId, Guid staffUserId, string body)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<ReplyConversationInput, ReplyConversationResult>>();
        return await uc.ExecuteAsync(new ReplyConversationInput(conversationId, staffUserId, body));
    }

    // ---- Reply ----

    [Fact]
    public async Task Reply_conversation_not_found_returns_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        h.Settings.Snapshot = SettingsFor(Guid.CreateVersion7(), 2000);

        var result = await ReplyAsync(h.Provider, Guid.CreateVersion7(), Guid.CreateVersion7(), "xin chao");
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_conversation_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Reply_missing_settings_returns_configuration_unavailable()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7());
        h.Settings.Snapshot = null;

        var result = await ReplyAsync(h.Provider, conversationId, Guid.CreateVersion7(), "xin chao");
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Reply_empty_body_returns_message_empty()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7());
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await ReplyAsync(h.Provider, conversationId, Guid.CreateVersion7(), "   ");
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_empty", result.Error.Code);
    }

    [Fact]
    public async Task Reply_too_long_returns_message_too_long()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7());
        h.Settings.Snapshot = SettingsFor(resortId, 10);

        var result = await ReplyAsync(h.Provider, conversationId, Guid.CreateVersion7(), new string('x', 11));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_too_long", result.Error.Code);
    }

    [Fact]
    public async Task Reply_appends_staff_message_without_incrementing_unread_and_notifies()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, visitId, guestMessages: 2);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);
        var staffUserId = Guid.CreateVersion7();

        var result = await ReplyAsync(h.Provider, conversationId, staffUserId, "le tan tra loi");
        Assert.True(result.IsSuccess);
        Assert.Equal(ConversationStatus.Open, result.Value.Status);
        Assert.Equal(1, h.Notifier.UpdatedCount);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(2, conv.UnreadForStaff); // KHÔNG tăng bởi tin của chính nhân viên.
        Assert.NotNull(conv.LastStaffMessageAt);
        var staffMsg = await db.Messages.SingleAsync(m => m.SenderType == MessageSenderType.Staff);
        Assert.Equal(staffUserId, staffMsg.SenderUserId);
        Assert.Equal("le tan tra loi", staffMsg.Body);
    }

    [Fact]
    public async Task Reply_reopens_closed_conversation()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7(), guestMessages: 1, status: ConversationStatus.Closed);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await ReplyAsync(h.Provider, conversationId, Guid.CreateVersion7(), "tiep tuc ho tro");
        Assert.True(result.IsSuccess);
        Assert.Equal(ConversationStatus.Open, result.Value.Status);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(ConversationStatus.Open, conv.Status);
        Assert.Null(conv.ClosedAt);
        Assert.Null(conv.ClosedByUserId);
    }

    // ---- MarkRead ----

    [Fact]
    public async Task MarkRead_conversation_not_found_returns_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<MarkConversationReadInput>>();
        var result = await uc.ExecuteAsync(new MarkConversationReadInput(Guid.CreateVersion7()));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_conversation_not_found", result.Error.Code);
    }

    [Fact]
    public async Task MarkRead_resets_unread_and_marks_guest_messages_read_and_notifies()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7(), guestMessages: 3);

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<MarkConversationReadInput>>();
            var result = await uc.ExecuteAsync(new MarkConversationReadInput(conversationId));
            Assert.True(result.IsSuccess);
        }

        Assert.Equal(1, h.Notifier.ReadCount);

        await using var s2 = h.Provider.CreateAsyncScope();
        var db = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(0, conv.UnreadForStaff);
        Assert.Equal(3, await db.Messages.CountAsync(m => m.ConversationId == conversationId && m.ReadByStaffAt != null));
    }

    // ---- Close ----

    [Fact]
    public async Task Close_conversation_not_found_returns_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<CloseConversationInput>>();
        var result = await uc.ExecuteAsync(new CloseConversationInput(Guid.CreateVersion7(), Guid.CreateVersion7()));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_conversation_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Close_sets_closed_with_staff_actor_and_notifies_then_idempotent()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, Guid.CreateVersion7());
        var staffUserId = Guid.CreateVersion7();

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<CloseConversationInput>>();
            Assert.True((await uc.ExecuteAsync(new CloseConversationInput(conversationId, staffUserId))).IsSuccess);
            // Đóng lần hai → idempotent Success, không đổi gì thêm.
            Assert.True((await uc.ExecuteAsync(new CloseConversationInput(conversationId, Guid.CreateVersion7()))).IsSuccess);
        }

        Assert.Equal(1, h.Notifier.UpdatedCount); // chỉ notify lần đóng thật đầu tiên.

        await using var s2 = h.Provider.CreateAsyncScope();
        var db = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(ConversationStatus.Closed, conv.Status);
        Assert.NotNull(conv.ClosedAt);
        Assert.Equal(staffUserId, conv.ClosedByUserId);
    }

    // ---- CloseForVisit (cascade C-GA.5) ----

    [Fact]
    public async Task CloseForVisit_no_conversation_is_noop_success()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<CloseConversationForVisitInput>>();
        var result = await uc.ExecuteAsync(new CloseConversationForVisitInput(Guid.CreateVersion7()));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, h.Notifier.UpdatedCount);
    }

    [Fact]
    public async Task CloseForVisit_already_closed_is_noop_success()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        await SeedConversationAsync(h.Provider, resortId, visitId, guestMessages: 1, status: ConversationStatus.Closed);

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<CloseConversationForVisitInput>>();
        var result = await uc.ExecuteAsync(new CloseConversationForVisitInput(visitId));
        Assert.True(result.IsSuccess);
        Assert.Equal(0, h.Notifier.UpdatedCount); // đã Closed → no-op.
    }

    [Fact]
    public async Task CloseForVisit_closes_open_conversation_by_system_with_null_actor()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId, visitId);

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<CloseConversationForVisitInput>>();
            Assert.True((await uc.ExecuteAsync(new CloseConversationForVisitInput(visitId))).IsSuccess);
        }

        Assert.Equal(1, h.Notifier.UpdatedCount);

        await using var s2 = h.Provider.CreateAsyncScope();
        var db = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(ConversationStatus.Closed, conv.Status);
        Assert.NotNull(conv.ClosedAt);
        Assert.Null(conv.ClosedByUserId); // System đóng → không actor.
    }
}
