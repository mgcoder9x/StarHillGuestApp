using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Concierge.Api.Realtime;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace StarHill.Api.Tests.Authorization;

/// <summary>
/// K-Con.4 — guard <see cref="SignalRConciergeNotifier"/>: mỗi sự kiện đẩy ĐÚNG method Server→Client tới ĐÚNG group
/// (conversation-{id} cho khách + resort-{resortId}-staff cho board). Bắt lỗi drift tên group/method (join một nơi,
/// push nơi khác = mất realtime im lặng). Fake <see cref="IHubContext{THub}"/> ghi lại (group, method). KHÔNG server SignalR.
/// </summary>
public sealed class SignalRConciergeNotifierTests
{
    private sealed class Recorder
    {
        public List<(string Group, string Method)> Sends { get; } = [];
    }

    private sealed class RecordingClientProxy(string group, Recorder recorder) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            recorder.Sends.Add((group, method));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubClients(Recorder recorder) : IHubClients
    {
        public IClientProxy All => throw new NotSupportedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => new RecordingClientProxy(groupName, recorder);
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class NoopGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHubContext : IHubContext<ChatHub>
    {
        public FakeHubContext(Recorder recorder) => Clients = new FakeHubClients(recorder);
        public IHubClients Clients { get; }
        public IGroupManager Groups { get; } = new NoopGroupManager();
    }

    [Fact]
    public async Task MessageReceived_pushed_to_conversation_and_staff_board()
    {
        var recorder = new Recorder();
        var notifier = new SignalRConciergeNotifier(new FakeHubContext(recorder));
        var resortId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();

        await notifier.NotifyMessageReceivedAsync(resortId, conversationId, Guid.CreateVersion7());

        Assert.Contains((ConciergeHubGroups.Conversation(conversationId), "MessageReceived"), recorder.Sends);
        Assert.Contains((ConciergeHubGroups.ResortStaff(resortId), "MessageReceived"), recorder.Sends);
        Assert.Equal(2, recorder.Sends.Count);
    }

    [Fact]
    public async Task ConversationUpdated_pushed_to_conversation_and_staff_board()
    {
        var recorder = new Recorder();
        var notifier = new SignalRConciergeNotifier(new FakeHubContext(recorder));
        var resortId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();

        await notifier.NotifyConversationUpdatedAsync(resortId, conversationId);

        Assert.Contains((ConciergeHubGroups.Conversation(conversationId), "ConversationUpdated"), recorder.Sends);
        Assert.Contains((ConciergeHubGroups.ResortStaff(resortId), "ConversationUpdated"), recorder.Sends);
        Assert.Equal(2, recorder.Sends.Count);
    }

    [Fact]
    public async Task MessageRead_pushed_to_conversation_only()
    {
        var recorder = new Recorder();
        var notifier = new SignalRConciergeNotifier(new FakeHubContext(recorder));
        var resortId = Guid.CreateVersion7();
        var conversationId = Guid.CreateVersion7();

        await notifier.NotifyMessageReadAsync(resortId, conversationId);

        Assert.Single(recorder.Sends);
        Assert.Equal((ConciergeHubGroups.Conversation(conversationId), "MessageRead"), recorder.Sends[0]);
    }
}
