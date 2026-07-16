using Housekeeping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Housekeeping.Infrastructure.Persistence.Configurations;

// RowVersion (xmin) do PlatformDbContext.OnModelCreating tự map cho entity IHasConcurrencyToken trên Npgsql (CP15).
// Id là UUIDv7 client-side → ValueGeneratedNever (mirror Rules/Faq/Rooms/GuestAccess).

/// <summary>
/// EF config <see cref="HousekeepingTicket"/> — partial unique "1 ticket MỞ/phòng" (<c>ux_hk_open_ticket_room</c>,
/// filter <c>status IN ('Requested','InProgress')</c> — enum lưu STRING nên filter đọc được + provider-agnostic,
/// mirror <c>ux_qr_active</c> Rooms). Index (resort, status) cho dashboard board. Status/CompletionMethod lưu string.
/// </summary>
public sealed class HousekeepingTicketConfiguration : IEntityTypeConfiguration<HousekeepingTicket>
{
    public void Configure(EntityTypeBuilder<HousekeepingTicket> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("housekeeping_ticket");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CompletionMethod).HasConversion<string>().HasMaxLength(16);

        // 1 ticket MỞ/phòng (Req 6.2) — filter trên cột enum-string (provider-agnostic; đặt ở config như ux_qr_active).
        builder.HasIndex(x => x.RoomId)
            .IsUnique()
            .HasDatabaseName("ux_hk_open_ticket_room")
            .HasFilter("status IN ('Requested','InProgress')");

        builder.HasIndex(x => new { x.ResortId, x.Status }).HasDatabaseName("ix_hk_ticket_resort_status");
    }
}

/// <summary>
/// EF config <see cref="HousekeepingEvent"/> — nhật ký append-only; FK→HousekeepingTicket Cascade (event thuộc ticket);
/// index (ticket, thời điểm) để đọc nhật ký đối soát. Status/Method lưu string.
/// </summary>
public sealed class HousekeepingEventConfiguration : IEntityTypeConfiguration<HousekeepingEvent>
{
    public void Configure(EntityTypeBuilder<HousekeepingEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("housekeeping_event");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => new { x.HousekeepingTicketId, x.CreatedAt }).HasDatabaseName("ix_hk_event_ticket");

        builder.HasOne<HousekeepingTicket>()
            .WithMany()
            .HasForeignKey(x => x.HousekeepingTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
