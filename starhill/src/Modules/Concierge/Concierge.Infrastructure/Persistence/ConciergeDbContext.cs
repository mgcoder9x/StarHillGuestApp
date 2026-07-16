using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Concierge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concierge.Infrastructure.Persistence;

/// <summary>
/// DbContext module Concierge — 1 DbContext + schema <c>concierge</c> (data ownership F31). KHÔNG outbox ở
/// K-Con.1..4 (cascade GuestVisitEnded là event PHÍA GuestAccess — C-GA.5). Index/constraint đặt ở EF config
/// (unique "1 hội thoại/visit" <c>ux_conversation_visit</c>; FK nội-schema Message→Conversation Cascade,
/// InternalNote→Conversation Restrict). ResortId/RoomId/GuestSessionId/GuestVisitId/SenderUserId/AuthorUserId/
/// ClosedByUserId là Guid TRẦN — KHÔNG FK chéo-schema (QR-AD-002).
/// </summary>
public sealed class ConciergeDbContext : PlatformDbContext
{
    public const string SchemaName = "concierge";

    public ConciergeDbContext(
        DbContextOptions<ConciergeDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConciergeDbContext).Assembly);
    }
}
