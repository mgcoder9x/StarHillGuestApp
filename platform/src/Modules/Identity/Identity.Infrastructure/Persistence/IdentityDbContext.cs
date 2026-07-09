using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Bedrock.Infrastructure.Persistence.Messaging;
using Bedrock.Infrastructure.Persistence.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext của module Identity — 1 DbContext + 1 schema <c>identity</c> (data ownership F31/I6, design §4.6).
/// Opt-in per-module: map Outbox/Inbox (state + event nguyên tử CÙNG transaction — CP6) và bảng refresh_token
/// (rotation nguyên tử) vào chính schema của module. Không có DbSet nghiệp vụ khác ở skeleton 16.1.
/// </summary>
public sealed class IdentityDbContext : PlatformDbContext
{
    public const string SchemaName = "identity";

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
    }
}
