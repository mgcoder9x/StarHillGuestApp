using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Rules.Domain;

namespace Rules.Infrastructure.Persistence;

/// <summary>
/// DbContext module Rules — 1 DbContext + schema <c>rules</c> (data ownership F31). KHÔNG outbox (Rules chưa phát
/// event). Index/constraint đặt ở EF config (partial unique IsCurrent filter provider-aware). ResortId/RoomId/
/// GuestVisitId/GuestSessionId là Guid trần — KHÔNG FK chéo-schema; FK nội-schema rules là hợp lệ.
/// </summary>
public sealed class RulesDbContext : PlatformDbContext
{
    public const string SchemaName = "rules";

    public RulesDbContext(
        DbContextOptions<RulesDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<RuleSet> RuleSets => Set<RuleSet>();
    public DbSet<RuleSection> RuleSections => Set<RuleSection>();
    public DbSet<RuleSectionTranslation> RuleSectionTranslations => Set<RuleSectionTranslation>();
    public DbSet<RulePublication> RulePublications => Set<RulePublication>();
    public DbSet<RulePublicationSection> RulePublicationSections => Set<RulePublicationSection>();
    public DbSet<RulePublicationSectionTranslation> RulePublicationSectionTranslations => Set<RulePublicationSectionTranslation>();
    public DbSet<RuleAcknowledgement> RuleAcknowledgements => Set<RuleAcknowledgement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RulesDbContext).Assembly);
    }
}
