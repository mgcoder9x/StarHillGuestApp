using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Infrastructure.Persistence.Security;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext của module Identity — 1 DbContext + 1 schema <c>identity</c> (data ownership F31/I6, design §4.6).
/// Opt-in per-module: map Outbox/Inbox (state + event nguyên tử CÙNG transaction — CP6) + bảng refresh_token
/// (rotation nguyên tử) + entity nghiệp vụ <see cref="IdentityUser"/> (F.1a — login) vào schema module.
/// </summary>
public sealed class IdentityDbContext : PlatformDbContext
{
    public const string SchemaName = "identity";

    public DbSet<IdentityUser> Users => Set<IdentityUser>();

    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddOutboxInbox(isNpgsql: Database.IsNpgsql(), schema: SchemaName);
        modelBuilder.AddRefreshTokens(schema: SchemaName);

        // F.1a: entity nghiệp vụ Identity (users) — quét IEntityTypeConfiguration trong assembly này (mirror Rules/Faq).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
