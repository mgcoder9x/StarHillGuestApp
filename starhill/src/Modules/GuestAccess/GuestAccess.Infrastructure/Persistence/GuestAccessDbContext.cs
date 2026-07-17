using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using GuestAccess.Domain;
using Microsoft.EntityFrameworkCore;

namespace GuestAccess.Infrastructure.Persistence;

/// <summary>
/// DbContext module GuestAccess — 1 DbContext + schema <c>guest_access</c> (data ownership F31). C-GA.5: map
/// Outbox/Inbox (<c>AddOutboxInbox</c>) vào schema module — GuestAccess vừa PRODUCE <c>GuestVisitEndedIntegrationEvent</c>
/// (outbox, cùng transaction chuyển visit→Expired) vừa là context CONSUME cascade (inbox idempotency, QR-AD-027).
/// Index/constraint đặt ở EF config (filter enum-string <c>status='Active'</c> provider-agnostic). ResortId/RoomId
/// là Guid trần — KHÔNG FK chéo-schema.
/// </summary>
public sealed class GuestAccessDbContext : PlatformDbContext
{
    public const string SchemaName = "guest_access";

    public GuestAccessDbContext(
        DbContextOptions<GuestAccessDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();
    public DbSet<GuestVisit> GuestVisits => Set<GuestVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        // C-GA.5: outbox (produce GuestVisitEnded) + inbox (consume cascade idempotent) vào schema guest_access.
        modelBuilder.AddOutboxInbox(isNpgsql: Database.IsNpgsql(), schema: SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuestAccessDbContext).Assembly);
    }
}
