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
/// K-Con.2b — use case GHI CHÚ NỘI BỘ Concierge (SQLite). Phủ create (empty/conversation-not-found/happy-room-only/
/// happy-conversation) + update (empty/not-found/happy-set-body+UpdatedAt) + delete (not-found/happy-hard-delete).
/// Validator (ít-nhất-phòng-hoặc-hội-thoại) đo riêng ở tầng validator. Logic provider-agnostic → SQLite cục bộ.
/// </summary>
public sealed class ConciergeNoteUseCaseTests
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
        public Task<ResortGuestConfig?> GetAsync(Guid resortId, CancellationToken ct = default) => Task.FromResult<ResortGuestConfig?>(null);
    }

    private sealed class StubSettingsQuery : IResortSettingsQuery
    {
        public Task<ResortSettingsSnapshot?> GetAsync(CancellationToken ct = default) => Task.FromResult<ResortSettingsSnapshot?>(null);
    }

    private sealed class FakeRuleGate : Rules.Contracts.IRuleGate
    {
        public Task<Result> EnsureAcknowledgedAsync(Guid resortId, Guid guestVisitId, Rules.Contracts.GuestFeature feature, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed record Harness(ServiceProvider Provider, SqliteConnection Connection);

    private static async Task<Harness> BuildAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new StubCurrentUser());
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<IResortGuestConfigQuery>(new StubGuestConfigQuery());
        services.AddSingleton<IResortSettingsQuery>(new StubSettingsQuery());
        services.AddSingleton<Rules.Contracts.IRuleGate>(new FakeRuleGate());
        services.AddConciergeInfrastructure(o => o.UseSqlite(connection));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new Harness(provider, connection);
    }

    private static async Task<Guid> SeedConversationAsync(ServiceProvider provider, Guid resortId)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var now = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var conversation = new Conversation
        {
            ResortId = resortId,
            RoomId = Guid.CreateVersion7(),
            GuestSessionId = Guid.CreateVersion7(),
            GuestVisitId = Guid.CreateVersion7(),
            Status = ConversationStatus.Open,
            LastMessageAt = now,
            CreatedAt = now,
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation.Id;
    }

    private static async Task<Result<CreateInternalNoteResult>> CreateAsync(ServiceProvider provider, CreateInternalNoteInput input)
    {
        await using var scope = provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<IUseCase<CreateInternalNoteInput, CreateInternalNoteResult>>();
        return await uc.ExecuteAsync(input);
    }

    // ---- Create ----

    [Fact]
    public async Task Create_empty_body_returns_message_empty()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        var result = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            Guid.CreateVersion7(), Guid.CreateVersion7(), null, Guid.CreateVersion7(), "   "));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_empty", result.Error.Code);
    }

    [Fact]
    public async Task Create_with_missing_conversation_returns_conversation_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        var result = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            Guid.CreateVersion7(), null, Guid.CreateVersion7(), Guid.CreateVersion7(), "ghi chu"));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_conversation_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Create_room_only_note_succeeds()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();

        var result = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            resortId, roomId, null, Guid.CreateVersion7(), "  khach VIP  "));
        Assert.True(result.IsSuccess);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var note = await db.InternalNotes.SingleAsync(n => n.Id == result.Value.NoteId);
        Assert.Equal(roomId, note.RoomId);
        Assert.Null(note.ConversationId);
        Assert.Equal("khach VIP", note.Body); // trim, plain text.
    }

    [Fact]
    public async Task Create_conversation_note_succeeds_when_conversation_exists()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();
        var conversationId = await SeedConversationAsync(h.Provider, resortId);

        var result = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            resortId, null, conversationId, Guid.CreateVersion7(), "theo doi hoi thoai"));
        Assert.True(result.IsSuccess);

        await using var scope = h.Provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var note = await db.InternalNotes.SingleAsync(n => n.Id == result.Value.NoteId);
        Assert.Equal(conversationId, note.ConversationId);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_not_found_returns_note_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateInternalNoteInput>>();
        var result = await uc.ExecuteAsync(new UpdateInternalNoteInput(Guid.CreateVersion7(), "noi dung moi"));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_note_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Update_empty_body_returns_message_empty()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateInternalNoteInput>>();
        var result = await uc.ExecuteAsync(new UpdateInternalNoteInput(Guid.CreateVersion7(), "   "));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_message_empty", result.Error.Code);
    }

    [Fact]
    public async Task Update_sets_body_and_updated_at()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();

        var created = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            resortId, Guid.CreateVersion7(), null, Guid.CreateVersion7(), "cu"));
        Assert.True(created.IsSuccess);

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<UpdateInternalNoteInput>>();
            var result = await uc.ExecuteAsync(new UpdateInternalNoteInput(created.Value.NoteId, "  moi  "));
            Assert.True(result.IsSuccess);
        }

        await using var s2 = h.Provider.CreateAsyncScope();
        var db = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        var note = await db.InternalNotes.SingleAsync(n => n.Id == created.Value.NoteId);
        Assert.Equal("moi", note.Body);
        Assert.NotNull(note.UpdatedAt);
    }

    // ---- Delete ----

    [Fact]
    public async Task Delete_not_found_returns_note_not_found()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;

        await using var scope = h.Provider.CreateAsyncScope();
        var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteInternalNoteInput>>();
        var result = await uc.ExecuteAsync(new DeleteInternalNoteInput(Guid.CreateVersion7()));
        Assert.False(result.IsSuccess);
        Assert.Equal("concierge_note_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Delete_removes_note()
    {
        var h = await BuildAsync();
        await using var _ = h.Provider; await using var __ = h.Connection;
        var resortId = Guid.CreateVersion7();

        var created = await CreateAsync(h.Provider, new CreateInternalNoteInput(
            resortId, Guid.CreateVersion7(), null, Guid.CreateVersion7(), "se xoa"));
        Assert.True(created.IsSuccess);

        await using (var scope = h.Provider.CreateAsyncScope())
        {
            var uc = scope.ServiceProvider.GetRequiredService<ICommandUseCase<DeleteInternalNoteInput>>();
            Assert.True((await uc.ExecuteAsync(new DeleteInternalNoteInput(created.Value.NoteId))).IsSuccess);
        }

        await using var s2 = h.Provider.CreateAsyncScope();
        var db = s2.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        Assert.False(await db.InternalNotes.AnyAsync(n => n.Id == created.Value.NoteId));
    }
}
