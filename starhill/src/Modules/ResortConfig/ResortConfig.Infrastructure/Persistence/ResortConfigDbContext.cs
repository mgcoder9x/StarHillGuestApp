using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ResortConfig.Domain;

namespace ResortConfig.Infrastructure.Persistence;

/// <summary>
/// DbContext của module ResortConfig — 1 DbContext + 1 schema <c>resort_config</c> (data ownership F31/§4.6).
/// KHÔNG map Outbox/Inbox/RefreshToken (QR-AD-008 — module không phát integration-event, không auth-token).
/// PlatformDbContext cấp cơ chế generic: snake_case, soft-delete filter, audit, concurrency xmin (Npgsql).
/// </summary>
public sealed class ResortConfigDbContext : PlatformDbContext
{
    public const string SchemaName = "resort_config";

    public ResortConfigDbContext(
        DbContextOptions<ResortConfigDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<Resort> Resorts => Set<Resort>();
    public DbSet<ResortLanguage> ResortLanguages => Set<ResortLanguage>();
    public DbSet<ResortSettings> ResortSettingsSet => Set<ResortSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResortConfigDbContext).Assembly);

        // Partial unique: đúng MỘT ngôn ngữ mặc định mỗi resort (CP14). Filter dùng cột snake_case (is_default).
        // Chỉ Npgsql (partial index qua HasFilter SQL) — provider khác bỏ qua để test provider-agnostic chạy được.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<ResortLanguage>()
                .HasIndex(x => x.ResortId)
                .IsUnique()
                .HasFilter("is_default")
                .HasDatabaseName("ux_lang_default");
        }
    }
}
