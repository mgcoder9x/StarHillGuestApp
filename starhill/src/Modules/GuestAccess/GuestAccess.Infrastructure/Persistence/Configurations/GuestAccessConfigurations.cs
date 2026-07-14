using GuestAccess.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GuestAccess.Infrastructure.Persistence.Configurations;

/// <summary>EF config GuestSession — hash cookie thiết bị unique (<c>ux_guest_session_key_hash</c>).</summary>
public sealed class GuestSessionConfiguration : IEntityTypeConfiguration<GuestSession>
{
    public void Configure(EntityTypeBuilder<GuestSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("guest_session");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SessionKeyHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(16);

        builder.HasIndex(x => x.SessionKeyHash)
            .IsUnique()
            .HasDatabaseName("ux_guest_session_key_hash");
    }
}

/// <summary>
/// EF config GuestVisit — partial unique 1 Active/(session,room) (<c>ux_guest_visit_active</c>); sweep index theo
/// <c>expires_at</c> (chỉ Active); check ràng buộc vòng đời (expires ≥ last_seen; Active↔closed_at null). FK
/// GuestSessionId→GuestSession Restrict (nội-module, cùng schema). Filter enum-string → provider-agnostic.
/// </summary>
public sealed class GuestVisitConfiguration : IEntityTypeConfiguration<GuestVisit>
{
    public void Configure(EntityTypeBuilder<GuestVisit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("guest_visit", t =>
        {
            // Vòng đời: hết hạn không thể trước lần thấy cuối; Active phải chưa đóng, đã kết thúc phải có mốc đóng.
            t.HasCheckConstraint("ck_guest_visit_expiry_after_seen", "expires_at >= last_seen_at");
            t.HasCheckConstraint(
                "ck_guest_visit_closed_at",
                "(status = 'Active' AND closed_at IS NULL) OR (status IN ('Closed', 'Expired') AND closed_at IS NOT NULL)");
        });

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => new { x.GuestSessionId, x.RoomId })
            .IsUnique()
            .HasDatabaseName("ux_guest_visit_active")
            .HasFilter("status = 'Active'");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_guest_visit_active_expiry")
            .HasFilter("status = 'Active'");

        builder.HasOne<GuestSession>()
            .WithMany()
            .HasForeignKey(x => x.GuestSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
