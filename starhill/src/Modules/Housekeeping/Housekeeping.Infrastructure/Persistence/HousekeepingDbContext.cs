using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Housekeeping.Domain;
using Microsoft.EntityFrameworkCore;

namespace Housekeeping.Infrastructure.Persistence;

/// <summary>
/// DbContext module Housekeeping — 1 DbContext + schema <c>housekeeping</c> (data ownership F31). KHÔNG outbox ở
/// H-Hk.1..3 (cascade GuestVisitEnded là event PHÍA GuestAccess — C-GA.5). Index/constraint đặt ở EF config
/// (partial unique "1 ticket mở/phòng" filter enum-string provider-agnostic). ResortId/RoomId/GuestVisitId/
/// GuestSessionId/CompletedByUserId là Guid trần — KHÔNG FK chéo-schema; FK nội-schema (event→ticket) hợp lệ.
/// </summary>
public sealed class HousekeepingDbContext : PlatformDbContext
{
    public const string SchemaName = "housekeeping";

    public HousekeepingDbContext(
        DbContextOptions<HousekeepingDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<HousekeepingTicket> HousekeepingTickets => Set<HousekeepingTicket>();
    public DbSet<HousekeepingEvent> HousekeepingEvents => Set<HousekeepingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HousekeepingDbContext).Assembly);
    }
}
