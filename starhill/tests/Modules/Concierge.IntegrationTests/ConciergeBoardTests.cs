using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Concierge.Application;
using Concierge.Domain;
using Concierge.Infrastructure.DependencyInjection;
using Concierge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Concierge.IntegrationTests;

/// <summary>
/// INTEGRATION (Testcontainers/PostgreSQL) cho board read-model <see cref="IConciergeReader"/> — SKIP nếu thiếu Docker.
/// Board sắp <c>LastMessageAt</c> DESC (mới-hoạt-động-nhất trước — Req 5.3): KHÔNG dùng Id-v7 vì LastMessageAt biến thiên
/// độc lập thời-điểm-TẠO (hội thoại tạo sớm nhưng vừa có tin mới phải lên đầu). SQLite KHÔNG ORDER BY DateTimeOffset
/// (bài học QR-N-059) → board đo trên Postgres (đường production). Phủ: order-desc + filter-status + phân-trang + chi-tiết.
/// </summary>
public sealed class ConciergeBoardTests : IAsyncLifetime
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

    private static Conversation NewConversation(
        Guid resortId, Guid visitId, DateTimeOffset lastMessageAt, ConversationStatus status = ConversationStatus.Open, int unread = 0) => new()
    {
        ResortId = resortId,
        RoomId = Guid.CreateVersion7(),
        GuestSessionId = Guid.CreateVersion7(),
        GuestVisitId = visitId,
        Status = status,
        LastMessageAt = lastMessageAt,
        LastGuestMessageAt = lastMessageAt,
        UnreadForStaff = unread,
        CreatedAt = lastMessageAt,
        ClosedAt = status == ConversationStatus.Closed ? lastMessageAt : null,
    };

    [SkippableFact]
    public async Task Board_orders_by_last_message_at_desc()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);
        var resortId = Guid.CreateVersion7();
        var baseTime = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        // Tạo theo thứ tự: A (cũ nhất theo Id) nhưng có LastMessageAt MỚI NHẤT → phải lên đầu board.
        var convA = NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(3));
        var convB = NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(1));
        var convC = NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(2));

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            db.Conversations.AddRange(convA, convB, convC);
            await db.SaveChangesAsync();
        }

        await using var readScope = provider.CreateAsyncScope();
        var reader = readScope.ServiceProvider.GetRequiredService<IConciergeReader>();
        var board = await reader.ListConversationsAsync(resortId, status: null, page: 1, pageSize: 20);

        Assert.Equal(3, board.TotalCount);
        Assert.Equal([convA.Id, convC.Id, convB.Id], board.Items.Select(i => i.ConversationId).ToArray());
    }

    [SkippableFact]
    public async Task Board_filters_by_status()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);
        var resortId = Guid.CreateVersion7();
        var baseTime = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var open = NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(1));
        var closed = NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(2), ConversationStatus.Closed);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            db.Conversations.AddRange(open, closed);
            await db.SaveChangesAsync();
        }

        await using var readScope = provider.CreateAsyncScope();
        var reader = readScope.ServiceProvider.GetRequiredService<IConciergeReader>();
        var openBoard = await reader.ListConversationsAsync(resortId, ConversationStatus.Open, 1, 20);

        Assert.Equal(1, openBoard.TotalCount);
        Assert.Equal(open.Id, openBoard.Items[0].ConversationId);
    }

    [SkippableFact]
    public async Task Board_paginates()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);
        var resortId = Guid.CreateVersion7();
        var baseTime = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            for (var i = 0; i < 3; i++)
            {
                db.Conversations.Add(NewConversation(resortId, Guid.CreateVersion7(), baseTime.AddHours(i)));
            }

            await db.SaveChangesAsync();
        }

        await using var readScope = provider.CreateAsyncScope();
        var reader = readScope.ServiceProvider.GetRequiredService<IConciergeReader>();
        var page1 = await reader.ListConversationsAsync(resortId, null, 1, 2);
        var page2 = await reader.ListConversationsAsync(resortId, null, 2, 2);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
    }

    [SkippableFact]
    public async Task GetConversation_returns_null_when_missing()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);

        await using var scope = provider.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IConciergeReader>();
        Assert.Null(await reader.GetConversationAsync(Guid.CreateVersion7()));
    }

    [SkippableFact]
    public async Task GetConversation_returns_detail_with_messages_and_sender()
    {
        Skip.IfNot(_available, "Docker/Postgres không khả dụng — bỏ qua integration test.");

        await using var provider = Build();
        await Migrate(provider);
        var resortId = Guid.CreateVersion7();
        var baseTime = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var conversation = NewConversation(resortId, Guid.CreateVersion7(), baseTime, unread: 1);
        var staffUserId = Guid.CreateVersion7();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConciergeDbContext>();
            db.Conversations.Add(conversation);
            db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderType = MessageSenderType.Guest,
                Body = "khach hoi",
                CreatedAt = baseTime,
            });
            db.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                SenderType = MessageSenderType.Staff,
                SenderUserId = staffUserId,
                Body = "le tan tra loi",
                CreatedAt = baseTime.AddMinutes(1),
            });
            await db.SaveChangesAsync();
        }

        await using var readScope = provider.CreateAsyncScope();
        var reader = readScope.ServiceProvider.GetRequiredService<IConciergeReader>();
        var detail = await reader.GetConversationAsync(conversation.Id);

        Assert.NotNull(detail);
        Assert.Equal(conversation.RoomId, detail!.RoomId);
        Assert.Equal(conversation.GuestVisitId, detail.GuestVisitId);
        Assert.Equal(1, detail.UnreadForStaff);
        Assert.Equal(2, detail.Messages.Count);
        // Tin sắp CŨ→MỚI theo Id-v7; tin nhân viên LỘ SenderUserId (staff-facing).
        var staffView = detail.Messages.Single(m => m.SenderType == MessageSenderType.Staff);
        Assert.Equal(staffUserId, staffView.SenderUserId);
        var guestView = detail.Messages.Single(m => m.SenderType == MessageSenderType.Guest);
        Assert.Null(guestView.SenderUserId);
    }
}
