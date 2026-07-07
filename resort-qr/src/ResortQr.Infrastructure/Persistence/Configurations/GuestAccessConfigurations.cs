using ResortQr.Domain.GuestAccess;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResortQr.Infrastructure.Persistence.Configurations;

/// <summary>EF config GuestSession — session_key_hash unique.</summary>
public sealed class GuestSessionConfiguration : IEntityTypeConfiguration<GuestSession>
{
    public void Configure(EntityTypeBuilder<GuestSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("guest_session");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionKeyHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(16);

        builder.HasIndex(x => x.SessionKeyHash).IsUnique().HasDatabaseName("ux_guestsession_key");
    }
}

/// <summary>
/// EF config GuestVisit — partial unique 1 Active/(session,room) + index sweep theo expires_at
/// (filter enum-string 'Active' → provider-agnostic). FK Restrict tới resort/room/guest_session.
/// </summary>
public sealed class GuestVisitConfiguration : IEntityTypeConfiguration<GuestVisit>
{
    public void Configure(EntityTypeBuilder<GuestVisit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("guest_visit");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => new { x.GuestSessionId, x.RoomId })
            .IsUnique()
            .HasDatabaseName("ux_visit_active")
            .HasFilter("status = 'Active'");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_visit_sweep")
            .HasFilter("status = 'Active'");

        builder.HasOne<Resort>()
            .WithMany()
            .HasForeignKey(x => x.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GuestSession>()
            .WithMany()
            .HasForeignKey(x => x.GuestSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
