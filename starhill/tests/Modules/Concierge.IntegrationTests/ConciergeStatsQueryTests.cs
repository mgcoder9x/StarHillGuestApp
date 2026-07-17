using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Concierge.Contracts;
using Concierge.Domain;
using Concierge.Infrastructure.DependencyInjection;
using Concierge.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Concierge.IntegrationTests;

/// <summary>
/// FE.0b — Ef stats query Concierge (SQLite local, đọc-đếm) cho Host Dashboard. Chứng minh LINQ CountAsync dịch đúng
/// (build KHÔNG kiểm dịch runtime): OpenConversations = Status=Open; UnreadConversations = số hội thoại UnreadForStaff>0;
/// scope theo ResortId (resort khác KHÔNG lẫn). Đại diện cho cùng khuôn 4 query-port stats (QR-AD-002).
/// </summary>
public sealed class ConciergeStatsQueryTests
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

    private static Conversation Conv(Guid resortId, ConversationStatus status, int unread) => new()
    {
        ResortId = resortId,
        RoomId = Guid.CreateVersion7(),
        GuestSessionId = Guid.CreateVersion7(),
        GuestVisitId = Guid.CreateVersion7(),
        Status = status,
        LastMessageAt = DateTimeOffset.UnixEpoch,
        UnreadForStaff = unread,
        CreatedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task Counts_open_and_unread_conversations_scoped_to_resort()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICurrentUser>(new StubCurrentUser());
            services.AddSingleton<IClock, FixedClock>();
            services.AddConciergeInfrastructure(o => o.UseSqlite(connection));
            await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            var resortA = Guid.CreateVersion7();
            var resortB = Guid.CreateVersion7();

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
                await db.Database.EnsureCreatedAsync();

                // resortA: 2 Open (1 có unread) + 1 Closed (có unread). resortB: 1 Open có unread (KHÔNG được lẫn).
                db.Conversations.Add(Conv(resortA, ConversationStatus.Open, unread: 4));
                db.Conversations.Add(Conv(resortA, ConversationStatus.Open, unread: 0));
                db.Conversations.Add(Conv(resortA, ConversationStatus.Closed, unread: 2));
                db.Conversations.Add(Conv(resortB, ConversationStatus.Open, unread: 9));
                await db.SaveChangesAsync();
            }

            await using (var scope = provider.CreateAsyncScope())
            {
                var query = scope.ServiceProvider.GetRequiredService<IConciergeStatsQuery>();
                var stats = await query.GetStatsAsync(resortA);

                Assert.Equal(2, stats.OpenConversations);   // 2 Open của resortA (không tính Closed, không tính resortB).
                Assert.Equal(2, stats.UnreadConversations); // 2 hội thoại resortA có UnreadForStaff>0 (Open-unread + Closed-unread).
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
