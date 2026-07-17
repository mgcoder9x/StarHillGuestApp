using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Concierge.Api.Realtime;
using Concierge.Application;
using Concierge.Domain;
using GuestAccess.Contracts;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using ResortConfig.Contracts.Queries;
using Bedrock.Domain.Results;
using StarHill.Authorization;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// K-Con.4 — guard AUTH-ON-JOIN của ChatHub (bản chất bảo mật Req 5.5/5.6): KHÁCH chỉ join hội thoại CỦA CHÍNH MÌNH
/// (server BỎ QUA conversationId client gửi → resolve cookie), NHÂN VIÊN join theo claim role. Test (a) authorizer
/// thuần + (b) hub thật với fake HubCallerContext/IGroupManager (không cần server SignalR). KHÔNG DB.
/// </summary>
public sealed class ChatHubAuthTests
{
    // ---- Fakes ----

    private sealed class ConfigurableResolver : ICurrentGuestContextResolver
    {
        public required Result<CurrentGuestContext> Next { get; set; }
        public Task<Result<CurrentGuestContext>> ResolveAsync(string? sessionKey, Guid roomId, CancellationToken ct = default) =>
            Task.FromResult(Next);
        public Task TouchAsync(Guid guestVisitId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class ConfigurableReader : IConciergeReader
    {
        public GuestConversationView? GuestConversation { get; set; }
        public Task<GuestConversationView?> GetGuestConversationByVisitAsync(Guid guestVisitId, CancellationToken ct = default) =>
            Task.FromResult(GuestConversation);
        public Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByGuestAsync(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> ListMessageIdsUnreadByStaffAsync(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<PagedConversations> ListConversationsAsync(Guid resortId, ConversationStatus? status, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new PagedConversations([], page, pageSize, 0));
        public Task<ConversationDetailView?> GetConversationAsync(Guid conversationId, CancellationToken ct = default) =>
            Task.FromResult<ConversationDetailView?>(null);
    }

    private sealed class StubSettingsQuery : IResortSettingsQuery
    {
        public static readonly Guid ResortId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public ResortSettingsSnapshot? Snapshot { get; set; } = new(
            ResortId, true, true, true, false, false, false, 30, 24, null, 2000, 10, 12);
        public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) => Task.FromResult(Snapshot);
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<string> Added { get; } = [];
        public List<string> Removed { get; } = [];
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add(groupName);
            return Task.CompletedTask;
        }
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Removed.Add(groupName);
            return Task.CompletedTask;
        }
    }

    // KHÔNG set IHttpContextFeature → Context.GetHttpContext() trả null → hub đọc cookie=null/roomId=Empty.
    // Bất biến bảo mật KHÔNG phụ thuộc cookie thật: hub KHÔNG truyền conversationId client vào authorizer;
    // fake resolver quyết định context → chứng minh khách chỉ join hội thoại do server resolve (không phải id client).
    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly IFeatureCollection _features = new FeatureCollection();

        public FakeHubCallerContext(ClaimsPrincipal? user) => User = user;

        public override string ConnectionId => "conn-1";
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User { get; }
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features => _features;
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private static ClaimsPrincipal StaffPrincipal() =>
        new(new ClaimsIdentity([new Claim("role", StarHillPolicies.RoleStaff)], "test"));

    private static ClaimsPrincipal AnonymousPrincipal() => new(new ClaimsIdentity());

    private static GuestConversationView ConversationView(Guid conversationId) =>
        new(conversationId, ConversationStatus.Open, DateTimeOffset.UnixEpoch, []);

    private static ChatHub BuildHub(
        ConfigurableResolver resolver, ConfigurableReader reader, StubSettingsQuery settings,
        ClaimsPrincipal? user, RecordingGroupManager groups)
    {
        var authorizer = new ConciergeHubAuthorizer(resolver, reader);
        return new ChatHub(authorizer, settings)
        {
            Context = new FakeHubCallerContext(user),
            Groups = groups,
        };
    }

    // ---- Authorizer (thuần) ----

    [Fact]
    public void IsStaff_true_for_role_claim_false_for_anonymous()
    {
        Assert.True(ConciergeHubAuthorizer.IsStaff(StaffPrincipal()));
        Assert.True(ConciergeHubAuthorizer.IsStaff(new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", StarHillPolicies.RoleAdmin)], "t"))));
        Assert.False(ConciergeHubAuthorizer.IsStaff(AnonymousPrincipal()));
        Assert.False(ConciergeHubAuthorizer.IsStaff(null));
    }

    [Fact]
    public async Task Authorizer_returns_own_conversation_when_context_valid()
    {
        var conversationId = Guid.CreateVersion7();
        var resolver = new ConfigurableResolver
        {
            Next = Result.Success(new CurrentGuestContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), StubSettingsQuery.ResortId)),
        };
        var reader = new ConfigurableReader { GuestConversation = ConversationView(conversationId) };
        var authorizer = new ConciergeHubAuthorizer(resolver, reader);

        var resolution = await authorizer.ResolveGuestConversationAsync("cookie", Guid.CreateVersion7());
        Assert.True(resolution.ContextValid);
        Assert.Equal(conversationId, resolution.ConversationId);
    }

    [Fact]
    public async Task Authorizer_context_invalid_when_resolve_fails()
    {
        var resolver = new ConfigurableResolver { Next = Result.Failure<CurrentGuestContext>(Error.Validation("guest_context_missing", "x")) };
        var authorizer = new ConciergeHubAuthorizer(resolver, new ConfigurableReader());

        var resolution = await authorizer.ResolveGuestConversationAsync(null, Guid.CreateVersion7());
        Assert.False(resolution.ContextValid);
        Assert.Null(resolution.ConversationId);
    }

    // ---- Hub JoinConversation ----

    [Fact]
    public async Task Guest_joins_only_own_conversation_ignoring_client_id()
    {
        var ownConversationId = Guid.CreateVersion7();
        var attackerRequestedId = Guid.CreateVersion7(); // id client tự bịa — PHẢI bị bỏ qua.
        var resolver = new ConfigurableResolver
        {
            Next = Result.Success(new CurrentGuestContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), StubSettingsQuery.ResortId)),
        };
        var reader = new ConfigurableReader { GuestConversation = ConversationView(ownConversationId) };
        var groups = new RecordingGroupManager();
        var hub = BuildHub(resolver, reader, new StubSettingsQuery(), AnonymousPrincipal(), groups);

        await hub.JoinConversation(attackerRequestedId);

        // Chỉ join hội thoại CỦA MÌNH (resolve), KHÔNG join id client gửi.
        Assert.Contains(ConciergeHubGroups.Conversation(ownConversationId), groups.Added);
        Assert.DoesNotContain(ConciergeHubGroups.Conversation(attackerRequestedId), groups.Added);
    }

    [Fact]
    public async Task Guest_with_invalid_context_throws_and_joins_nothing()
    {
        var resolver = new ConfigurableResolver { Next = Result.Failure<CurrentGuestContext>(Error.Validation("guest_context_missing", "x")) };
        var groups = new RecordingGroupManager();
        var hub = BuildHub(resolver, new ConfigurableReader(), new StubSettingsQuery(), AnonymousPrincipal(), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinConversation(Guid.CreateVersion7()));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Staff_joins_requested_conversation()
    {
        var requestedId = Guid.CreateVersion7();
        var groups = new RecordingGroupManager();
        var hub = BuildHub(new ConfigurableResolver { Next = Result.Failure<CurrentGuestContext>(Error.Validation("x", "x")) },
            new ConfigurableReader(), new StubSettingsQuery(), StaffPrincipal(), groups);

        await hub.JoinConversation(requestedId);

        Assert.Contains(ConciergeHubGroups.Conversation(requestedId), groups.Added);
    }

    // ---- Hub JoinBoard ----

    [Fact]
    public async Task Staff_joins_resort_board_group()
    {
        var groups = new RecordingGroupManager();
        var hub = BuildHub(new ConfigurableResolver { Next = Result.Failure<CurrentGuestContext>(Error.Validation("x", "x")) },
            new ConfigurableReader(), new StubSettingsQuery(), StaffPrincipal(), groups);

        await hub.JoinBoard();

        Assert.Contains(ConciergeHubGroups.ResortStaff(StubSettingsQuery.ResortId), groups.Added);
    }

    [Fact]
    public async Task Guest_cannot_join_board()
    {
        var groups = new RecordingGroupManager();
        var hub = BuildHub(new ConfigurableResolver { Next = Result.Failure<CurrentGuestContext>(Error.Validation("x", "x")) },
            new ConfigurableReader(), new StubSettingsQuery(), AnonymousPrincipal(), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinBoard());
        Assert.Empty(groups.Added);
    }
}
