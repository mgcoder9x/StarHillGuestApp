using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence.Security;

/// <summary>
/// Map bảng <c>refresh_token</c> vào CHÍNH DbContext/schema của module tiêu thụ (per-module, giống
/// <c>AddOutboxInbox</c> — data ownership F31/AD-010). Module gọi trong <c>OnModelCreating</c>. Method public
/// nhưng KHÔNG lộ <c>RefreshTokenRecord</c> (internal) ra ngoài — encapsulation F19 giữ nguyên.
/// </summary>
public static class RefreshTokenModelBuilderExtensions
{
    public static ModelBuilder AddRefreshTokens(this ModelBuilder modelBuilder, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<RefreshTokenRecord>(entity =>
        {
            entity.ToTable("refresh_token", schema);
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedNever(); // UUIDv7 sinh client-side.

            entity.Property(r => r.UserId).IsRequired();
            entity.Property(r => r.FamilyId).IsRequired();
            entity.Property(r => r.TokenHash).IsRequired().HasMaxLength(64); // SHA-256 hex = 64 ký tự.
            entity.Property(r => r.ExpiresAt).IsRequired();
            entity.Property(r => r.CreatedAt).IsRequired();
            entity.Property(r => r.RevokedReason).HasMaxLength(64);

            // F10: hash DUY NHẤT ở cấp DB (ux_refresh_hash) — chống trùng token. Lookup hot-path lúc refresh.
            entity.HasIndex(r => r.TokenHash).IsUnique().HasDatabaseName("ux_refresh_hash");
            entity.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_user");
            entity.HasIndex(r => r.FamilyId).HasDatabaseName("ix_refresh_family");
        });

        return modelBuilder;
    }
}
