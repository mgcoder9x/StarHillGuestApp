using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Concierge.Domain;
using Concierge.Infrastructure.DependencyInjection;
using Concierge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Concierge.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho ràng buộc DB module Concierge — áp migration THẬT (đường production).
/// SKIP nếu thiếu Docker (N-067). Phủ: đúng MỘT hội thoại/visit (unique <c>ux_conversation_visit</c> — Req 5.2);
/// FK Cascade Message→Conversation (xoá hội thoại → xoá tin); FK Restrict InternalNote→Conversation (không xoá hội
/// thoại còn ghi chú trỏ tới — giữ vết). K-Con.1 = persistence nền.
/// </summary>
public sealed class ConciergePostgresConstraintTests : IAsyncLifetime
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
        services.AddConciergeInfrastructure(o => o.UseNpgsql(
            _container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "concierge")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static async Task Migrate(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
        await db.Database.MigrateAsync();
    }

    private static Conversation NewConversation(Guid resortId, Guid roomId, Guid sessionId, Guid visitId) => new()
    {
        ResortId = resortId,
        RoomId = roomId,
        GuestSessionId = sessionId,
        GuestVisitId = visitId,
        Status = ConversationStatus.Open,
        LastMessageAt = DateTimeOffset.UnixEpoch,
        CreatedAt = DateTimeOffset.UnixEpoch,
    };

    [SkippableFact]
    public async Task Only_one_conversation_per_visit()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        var resortId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var visitId = Guid.CreateVersion7();

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();

        db.Conversations.Add(NewConversation(resortId, roomId, Guid.CreateVersion7(), visitId));
        await db.SaveChangesAsync();

        // Hội thoại thứ hai cùng GuestVisitId → vi phạm ux_conversation_visit.
        db.Conversations.Add(NewConversation(resortId, roomId, Guid.CreateVersion7(), visitId));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Deleting_conversation_cascades_messages()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        Guid conversationId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var conversation = NewConversation(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
            conversationId = conversation.Id;
            db.Conversations.Add(conversation);
            db.Messages.Add(new Message
            {
                ConversationId = conversationId,
                SenderType = MessageSenderType.Guest,
                Body = "xin chao",
                CreatedAt = DateTimeOffset.UnixEpoch,
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var conversation = await db.Conversations.FirstAsync(c => c.Id == conversationId);
            db.Conversations.Remove(conversation);
            await db.SaveChangesAsync(); // Cascade → tin bị xoá theo.

            Assert.False(await db.Messages.AnyAsync(m => m.ConversationId == conversationId));
        }
    }

    [SkippableFact]
    public async Task Cannot_delete_conversation_referenced_by_note()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        Guid conversationId;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var conversation = NewConversation(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
            conversationId = conversation.Id;
            db.Conversations.Add(conversation);
            db.InternalNotes.Add(new InternalNote
            {
                ResortId = conversation.ResortId,
                ConversationId = conversationId,
                AuthorUserId = Guid.CreateVersion7(),
                Body = "ghi chu noi bo",
                CreatedAt = DateTimeOffset.UnixEpoch,
            });
            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            var conversation = await db.Conversations.FirstAsync(c => c.Id == conversationId);
            db.Conversations.Remove(conversation);
            // FK Restrict → không xoá được hội thoại còn ghi chú trỏ tới.
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
