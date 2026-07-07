using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResortQr.Infrastructure.Persistence.Configurations;

/// <summary>EF config Room — status enum→string; partial unique room_number ở AppDbContext (bool is_deleted provider-aware).</summary>
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

        builder.HasOne<Resort>()
            .WithMany()
            .HasForeignKey(x => x.ResortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// EF config RoomQrToken — token unique toàn cục; partial unique 1 token Active/phòng (filter enum-string
/// → provider-agnostic, đặt ngay ở config).
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
