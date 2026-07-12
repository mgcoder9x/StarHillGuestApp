using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rooms.Domain;

namespace Rooms.Infrastructure.Persistence.Configurations;

/// <summary>EF config Room — status enum→string; ux_room_number (partial) đặt ở DbContext (Npgsql-aware). KHÔNG FK
/// chéo-schema Room→Resort (QR-DV-003 — ResortId Guid trần).</summary>
public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("room");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RoomNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Building).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
    }
}

/// <summary>
/// EF config RoomQrToken — token unique toàn cục (ux_qrtoken_token); partial unique 1 token Active/phòng
/// (ux_qr_active, filter enum-string → provider-agnostic, đặt ngay ở config). FK RoomId→Room (nội-module, cùng schema).
/// </summary>
public sealed class RoomQrTokenConfiguration : IEntityTypeConfiguration<RoomQrToken>
{
    public void Configure(EntityTypeBuilder<RoomQrToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("room_qr_token");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Token).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TokenPreview).IsRequired().HasMaxLength(32);
        builder.Property(x => x.RevocationReason).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => x.Token).IsUnique().HasDatabaseName("ux_qrtoken_token");

        builder.HasIndex(x => x.RoomId)
            .IsUnique()
            .HasDatabaseName("ux_qr_active")
            .HasFilter("status = 'Active'");

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
