using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Rooms.Domain;

namespace Rooms.Infrastructure.Persistence;

/// <summary>
/// DbContext module Rooms — 1 DbContext + schema <c>rooms</c> (data ownership F31). KHÔNG outbox (chưa phát event).
/// Partial unique <c>ux_room_number</c> (theo resort, chỉ hàng chưa xóa) đặt ở đây (Npgsql-aware — filter bool khác
/// provider). Các index còn lại (ux_qrtoken_token, ux_qr_active) đặt ở EF config (provider-agnostic).
/// </summary>
public sealed class RoomsDbContext : PlatformDbContext
{
    public const string SchemaName = "rooms";

    public RoomsDbContext(
        DbContextOptions<RoomsDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomQrToken> RoomQrTokens => Set<RoomQrToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoomsDbContext).Assembly);

        // Partial unique: số phòng duy nhất theo resort TRONG PHẠM VI chưa xóa mềm (Req 16.6). Filter bool
        // Postgres-specific ("is_deleted = false") → chỉ Npgsql; provider khác (SQLite test) bỏ qua (không test luật này ở SQLite).
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<Room>()
                .HasIndex(x => new { x.ResortId, x.RoomNumber })
                .IsUnique()
                .HasFilter("is_deleted = false")
                .HasDatabaseName("ux_room_number");
        }
    }
}
