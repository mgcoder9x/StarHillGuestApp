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
/// K-Con.2a — use case guest Concierge (SQLite + fake config/settings/gate + spy notifier). Phủ: send (config-null/
/// chat-disabled/gate-fail/empty/too-long/happy/append-idempotent-1-hội-thoại/reopen/plain-text-không-sanitize/
/// notifier-sau-persist) + get (null/scope-theo-visit/mark-ReadByGuest). Logic Application provider-agnostic → SQLite
/// chạy cục bộ; unique-visit + race đo riêng Postgres (ConciergePostgresConstraintTests).
/// </summary>
public sealed class ConciergeUseCaseTests
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
        public Guid LastConversationId { get; private set; }
        public Task NotifyMessageReceivedAsync(Guid resortId, Guid conversationId, Guid messageId, CancellationToken ct = default)
        {
            ReceivedCount++;
            LastConversationId = conversationId;
            return Task.CompletedTask;
        }
        public Task NotifyConversationUpdatedAsync(Guid resortId, Guid conversationId, CancellationToken ct = default) => Task.CompletedTask;
        public Task NotifyMessageReadAsync(Guid resortId, Guid conversationId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ResortGuestConfig ConfigFor(Guid resortId, bool chatEnabled) => new(
        resortId, "Star Hill", null, ["en"], "en", true, chatEnabled, true, false, false, false, 30, 24);

    private static ResortSettingsSnapshot SettingsFor(Guid resortId, int maxMessageLength) => new(
        resortId, true, true, true, false, false, false, 30, 24, null, maxMessageLength, 10, 12);

    private sealed record Harness(
        ServiceProvider Provider, SqliteConnection Connection, StubGuestConfigQuery Config, StubSettingsQuery Settings, FakeRuleGate Gate, SpyNotifier Notifier);

    private static async Task<Harness> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var config = new StubGuestConfigQuery();
        var settings = new StubSettingsQuery();
        var gate = new FakeRuleGate();
        var notifier = new SpyNotifier();
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<IResortGuestConfigQuery>(config);
        services.AddSingleton<IResortSettingsQuery>(settings);
        services.AddSingleton<Rules.Contracts.IRuleGate>(gate);
        services.AddConciergeInfrastructure(o => o.UseSqlite(connection));
        // Override notifier no-op mặc định bằng spy (đăng ký SAU → last-registration-wins).
        services.AddSingleton<IConciergeRealtimeNotifier>(notifier);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new Harness(provider, connection, config, settings, gate, notifier);
    }

    private static async Task<Result<SendGuestMessageResult>> SendAsync(
        ServiceProvider provider, Guid resortId, Guid roomId, Guid visitId, string body, Guid? sessionId = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<SendGuestMessageInput, SendGuestMessageResult>>();
        return await uc.ExecuteAsync(new SendGuestMessageInput(resortId, roomId, sessionId ?? Guid.CreateVersion7(), visitId, body));
    }

    private static async Task<Result<GetGuestConversationResult>> GetAsync(ServiceProvider provider, Guid visitId)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<GetGuestConversationInput, GetGuestConversationResult>>();
        return await uc.ExecuteAsync(new GetGuestConversationInput(visitId));
    }

    [Fact]
    public async Task Send_missing_config_returns_configuration_unavailable()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        h.Config.Config = null;

        var result = await SendAsync(h.Provider, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "xin chao");
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task Send_chat_disabled_returns_chat_disabled()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: false);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7(), "xin chao");
        Assert.False(result.IsSuccess);
        Assert.Equal("chat_disabled", result.Error.Code);
    }

    [Fact]
    public async Task Send_gate_failure_returns_rule_ack_required()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);
        h.Gate.Next = Result.Failure(Error.Forbidden("rule_ack_required", "chưa ack"));

        var result = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7(), "xin chao");
        Assert.False(result.IsSuccess);
        Assert.Equal("rule_ack_required", result.Error.Code);
    }

    [Fact]
    public async Task Send_empty_body_returns_message_empty()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7(), "   ");
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_empty", result.Error.Code);
    }

    [Fact]
    public async Task Send_too_long_returns_message_too_long()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 10);

        var result = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), Guid.CreateVersion7(), new string('x', 11));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_too_long", result.Error.Code);
    }

    [Fact]
    public async Task Send_creates_open_conversation_with_unread_one()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await SendAsync(h.Provider, resortId, roomId, visitId, "xin chao");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Reopened);
        Assert.Equal(ConversationStatus.Open, result.Value.Status);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var conv = await db.Conversations.SingleAsync(c => c.GuestVisitId == visitId);
        Assert.Equal(1, conv.UnreadForStaff);
        Assert.Equal(ConversationStatus.Open, conv.Status);
        Assert.Equal(1, await db.Messages.CountAsync(m => m.ConversationId == conv.Id));
    }

    [Fact]
    public async Task Send_second_message_appends_to_same_conversation_and_increments_unread()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var first = await SendAsync(h.Provider, resortId, roomId, visitId, "tin 1");
        var second = await SendAsync(h.Provider, resortId, roomId, visitId, "tin 2");
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.ConversationId, second.Value.ConversationId); // 1 hội thoại/visit — không tạo mới.

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        Assert.Equal(1, await db.Conversations.CountAsync(c => c.GuestVisitId == visitId));
        var conv = await db.Conversations.SingleAsync(c => c.GuestVisitId == visitId);
        Assert.Equal(2, conv.UnreadForStaff);
        Assert.Equal(2, await db.Messages.CountAsync(m => m.ConversationId == conv.Id));
    }

    [Fact]
    public async Task Send_after_close_reopens_same_conversation()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var first = await SendAsync(h.Provider, resortId, roomId, visitId, "tin 1");

        // Đóng hội thoại thủ công (mô phỏng staff close — CloseConversation ở K-Con.2b).
        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var conv = await db.Conversations.SingleAsync(c => c.GuestVisitId == visitId);
            conv.Status = ConversationStatus.Closed;
            conv.ClosedAt = new DateTimeOffset(2026, 1, 1, 13, 0, 0, TimeSpan.Zero);
            conv.ClosedByUserId = Guid.CreateVersion7();
            await db.SaveChangesAsync();
        }

        var reopen = await SendAsync(h.Provider, resortId, roomId, visitId, "tin 2 mở lại");
        Assert.True(reopen.IsSuccess);
        Assert.True(reopen.Value.Reopened);
        Assert.Equal(first.Value.ConversationId, reopen.Value.ConversationId); // cùng hội thoại, không tạo mới.
        Assert.Equal(ConversationStatus.Open, reopen.Value.Status);

        await using var s2 = h.Provider.CreateAsyncScope();
        var db2 = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var reopened = await db2.Conversations.SingleAsync(c => c.GuestVisitId == visitId);
        Assert.Equal(ConversationStatus.Open, reopened.Status);
        Assert.Null(reopened.ClosedAt);
        Assert.Null(reopened.ClosedByUserId);
    }

    [Fact]
    public async Task Send_stores_body_plaintext_without_sanitize()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);
        const string raw = "<script>alert(1)</script> a < b & c > d";

        await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), visitId, "  " + raw + "  ");

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var message = await db.Messages.SingleAsync();
        // Body lưu NGUYÊN (chỉ trim), KHÔNG HTML-sanitize (khác Rules/FAQ) — client render textContent.
        Assert.Equal(raw, message.Body);
    }

    [Fact]
    public async Task Send_notifies_message_received_once_after_persist()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);

        var result = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), visitId, "xin chao");
        Assert.True(result.IsSuccess);
        Assert.Equal(1, h.Notifier.ReceivedCount);
        Assert.Equal(result.Value.ConversationId, h.Notifier.LastConversationId);
    }

    [Fact]
    public async Task Get_conversation_returns_null_when_visit_has_none()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        var result = await GetAsync(h.Provider, Guid.CreateVersion7());
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Conversation);
    }

    [Fact]
    public async Task Get_conversation_is_scoped_to_visit()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);
        var visitA = Guid.CreateVersion7();
        var visitB = Guid.CreateVersion7();
        await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), visitA, "tin cua visit A");

        // Visit B (quét lại) KHÔNG thấy hội thoại của visit A (Req 5.9).
        var resultB = await GetAsync(h.Provider, visitB);
        Assert.Null(resultB.Value.Conversation);

        var resultA = await GetAsync(h.Provider, visitA);
        Assert.NotNull(resultA.Value.Conversation);
        Assert.Single(resultA.Value.Conversation!.Messages);
        Assert.Equal("tin cua visit A", resultA.Value.Conversation.Messages[0].Body);
    }

    [Fact]
    public async Task Get_conversation_marks_staff_messages_read_by_guest()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();
        h.Config.Config = ConfigFor(resortId, chatEnabled: true);
        h.Settings.Snapshot = SettingsFor(resortId, 2000);
        var send = await SendAsync(h.Provider, resortId, Guid.CreateVersion7(), visitId, "khach hoi");
        var conversationId = send.Value.ConversationId;

        // Chèn tin nhân viên (mô phỏng reply — ReplyConversation ở K-Con.2b) chưa được khách đọc.
        Guid staffMessageId;
        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var staffMsg = new Message
            {
                ConversationId = conversationId,
                SenderType = MessageSenderType.Staff,
                SenderUserId = Guid.CreateVersion7(),
                Body = "le tan tra loi",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 12, 5, 0, TimeSpan.Zero),
            };
            db.Messages.Add(staffMsg);
            await db.SaveChangesAsync();
            staffMessageId = staffMsg.Id;
        }

        // Khách mở hội thoại → tin nhân viên được đánh dấu ReadByGuest.
        var get = await GetAsync(h.Provider, visitId);
        Assert.True(get.IsSuccess);

        await using var s2 = h.Provider.CreateAsyncScope();
        var db2 = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var staffMessage = await db2.Messages.SingleAsync(m => m.Id == staffMessageId);
        Assert.NotNull(staffMessage.ReadByGuestAt);
    }
}
