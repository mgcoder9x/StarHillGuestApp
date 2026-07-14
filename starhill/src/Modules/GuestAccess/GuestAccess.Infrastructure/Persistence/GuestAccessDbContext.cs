using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using GuestAccess.Domain;
using Microsoft.EntityFrameworkCore;

namespace GuestAccess.Infrastructure.Persistence;

/// <summary>
/// DbContext module GuestAccess — 1 DbContext + schema <c>guest_access</c> (data ownership F31). KHÔNG outbox ở
/// slice resolve (thêm khi cascade GuestVisitEnded có consumer — QR-AD-027). Index/constraint đặt ở EF config
/// (filter enum-string <c>status='Active'</c> provider-agnostic). ResortId/RoomId là Guid trần — KHÔNG FK chéo-schema.
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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuestAccessDbContext).Assembly);
    }
}
