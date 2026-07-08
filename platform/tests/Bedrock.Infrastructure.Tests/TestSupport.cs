using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Domain.Entities;
using Bedrock.Domain.Events;
using Bedrock.Infrastructure.DependencyInjection;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Messaging.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bedrock.Infrastructure.Tests;

// ── Domain events dùng cho test ─────────────────────────────────────────────
public sealed record TestThingCreated(Guid ThingId) : IDomainEvent;

public sealed record ChainEvent : IDomainEvent;

// ── Integration event dùng cho test outbox ──────────────────────────────────
public sealed record ThingHappened(Guid Id, DateTimeOffset OccurredAt, string Name)
    : IntegrationEvent(Id, OccurredAt)
{
    public override string EventType => "test.thing_happened";
}

// ── Entity test: đủ 3 khả năng để phủ audit + soft-delete + concurrency token ──
public sealed class TestThing : AuditableEntity, ISoftDeletable, IHasConcurrencyToken
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public uint RowVersion { get; set; }

    public void EmitCreated() => RaiseDomainEvent(new TestThingCreated(Id));

    public void EmitChain() => RaiseDomainEvent(new ChainEvent());
}

// Entity side-effect (KHÔNG soft-deletable) — dùng để chứng minh handler ghi cùng transaction + map concurrency.
public sealed class TestLog : Entity
{
    public string Message { get; set; } = string.Empty;
}

// ── DbContext test dẫn xuất PlatformDbContext ───────────────────────────────
public sealed class TestDbContext(
    DbContextOptions<TestDbContext> options,
    IClock clock,
    ICurrentUser currentUser,
    IDomainEventDispatcher dispatcher)
    : PlatformDbContext(options, clock, currentUser, dispatcher)
{
    public DbSet<TestThing> Things => Set<TestThing>();

    public DbSet<TestLog> Logs => Set<TestLog>();

    /// <summary>Cho phép test đặt trần dispatch nhỏ để kiểm max-depth mà không cần 25 vòng.</summary>
    public int? MaxDepthOverride { get; set; }

    protected override int MaxDomainEventDispatchDepth => MaxDepthOverride ?? base.MaxDomainEventDispatchDepth;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Per-module opt-in (design §4.6): map outbox/inbox vào chính DbContext này. SQLite → không jsonb.
        modelBuilder.AddOutboxInbox(isNpgsql: Database.IsNpgsql());
    }
}

// ── Test doubles cho port ───────────────────────────────────────────────────
public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
}

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated => UserId is not null;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
    public Guid? TenantId { get; set; }
    public Guid? SessionId { get; set; }
    public bool IsInRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

// ── Handler test ────────────────────────────────────────────────────────────
/// <summary>Handler ghi một TestLog khi nhận TestThingCreated — chứng minh hiệu ứng handler enlist cùng transaction.</summary>
public sealed class SideEffectHandler(TestDbContext db) : IDomainEventHandler<TestThingCreated>
{
    public Task HandleAsync(TestThingCreated domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        db.Logs.Add(new TestLog { Message = $"created:{domainEvent.ThingId}" });
        return Task.CompletedTask;
    }
}

/// <summary>Handler luôn ném — chứng minh handler lỗi → không gì được commit (CP14/R33.2).</summary>
public sealed class ThrowingHandler : IDomainEventHandler<TestThingCreated>
{
    public const string Message = "handler cố tình ném để test rollback";

    public Task HandleAsync(TestThingCreated domainEvent, CancellationToken ct = default) =>
        throw new InvalidOperationException(Message);
}

/// <summary>Handler tự sinh event mới mỗi vòng → kích hoạt biên MaxDispatchDepth (R33.4).</summary>
public sealed class ChainHandler(TestDbContext db) : IDomainEventHandler<ChainEvent>
{
    public Task HandleAsync(ChainEvent domainEvent, CancellationToken ct = default)
    {
        var next = new TestThing { Name = "chain" };
        next.EmitChain();
        db.Things.Add(next);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Harness: SQLite in-memory (connection giữ mở suốt vòng đời) + DI thật qua <c>AddBedrockPersistence</c>
/// → test wiring end-to-end, KHÔNG cần Docker. Postgres-specific (xmin/partial-index) hoãn Testcontainers.
/// </summary>
public sealed class PersistenceHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private PersistenceHarness(SqliteConnection connection, ServiceProvider provider, TestClock clock, TestCurrentUser user)
    {
        _connection = connection;
        Provider = provider;
        Clock = clock;
        User = user;
    }

    public ServiceProvider Provider { get; }

    public TestClock Clock { get; }

    public TestCurrentUser User { get; }

    public static async Task<PersistenceHarness> CreateAsync(Action<IServiceCollection>? configure = null)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var clock = new TestClock();
        var user = new TestCurrentUser { UserId = Guid.CreateVersion7() };

        var services = new ServiceCollection();
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ICurrentUser>(user);
        services.AddBedrockPersistence<TestDbContext>(options => options.UseSqlite(connection));
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }

        return new PersistenceHarness(connection, provider, clock, user);
    }

    public AsyncServiceScope CreateScope() => Provider.CreateAsyncScope();

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
