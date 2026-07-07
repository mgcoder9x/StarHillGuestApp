using ResortQr.Application.Abstractions;
using ResortQr.Domain.GuestAccess;
using ResortQr.Domain.Identity;
using ResortQr.Domain.Resorts;
using ResortQr.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace ResortQr.Infrastructure.Persistence;

/// <summary>
/// DbContext app: dẫn xuất <see cref="ResortQrDbContext"/> (audit/soft-delete/xmin-Npgsql/snake_case).
/// Base tự nạp mọi <c>IEntityTypeConfiguration</c> cùng assembly + convention. Ở đây chỉ thêm 2 partial index
/// có filter BOOL (khác cú pháp SQLite/Npgsql → provider-aware, không hardcode).
/// </summary>
public sealed class AppDbContext : ResortQrDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider clock, ICurrentUser currentUser)
        : base(options, clock, currentUser)
    {
    }

    public DbSet<Resort> Resorts => Set<Resort>();
    public DbSet<ResortSettings> ResortSettingsSet => Set<ResortSettings>();
    public DbSet<ResortLanguage> ResortLanguages => Set<ResortLanguage>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomQrToken> RoomQrTokens => Set<RoomQrToken>();
    public DbSet<GuestSession> GuestSessions => Set<GuestSession>();
    public DbSet<GuestVisit> GuestVisits => Set<GuestVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        var isNpgsql = Database.IsNpgsql();

        // Số phòng unique theo resort trong phạm vi CHƯA xóa mềm (cho phép tái dùng số sau khi xóa).
        modelBuilder.Entity<Room>()
            .HasIndex(r => new { r.ResortId, r.RoomNumber })
            .IsUnique()
            .HasDatabaseName("ux_room_number")
            .HasFilter(BoolEquals(isNpgsql, "is_deleted", value: false));

        // Đúng một ngôn ngữ mặc định mỗi resort.
        modelBuilder.Entity<ResortLanguage>()
            .HasIndex(l => l.ResortId)
            .IsUnique()
            .HasDatabaseName("ux_lang_default")
            .HasFilter(BoolEquals(isNpgsql, "is_default", value: true));
    }

    /// <summary>
    /// Sinh predicate boolean literal đúng theo provider: Npgsql dùng <c>true/false</c>, SQLite dùng <c>1/0</c>.
    /// Cột đã snake_case (convention). Fix gốc như xmin — không hardcode 1 cú pháp.
    /// </summary>
    private static string BoolEquals(bool isNpgsql, string column, bool value)
    {
        var literal = isNpgsql ? (value ? "true" : "false") : (value ? "1" : "0");
        return $"{column} = {literal}";
    }
}
