using Foundation.Application.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foundation.Infrastructure.Persistence;

/// <summary>
/// Map <see cref="RefreshTokenRecord"/> (persistence-facing) → bảng <c>refresh_token</c>.
/// Chỉ lưu HASH token (không secret thô). Index theo hash phục vụ lookup lúc refresh (04 §7.2 ix_refresh_hash).
/// KHÔNG audit/xmin (04 §8: refresh token là tạo/thu hồi, không sửa nội dung → không cần concurrency token).
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenRecord>
{
    public void Configure(EntityTypeBuilder<RefreshTokenRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_token");

        builder.HasKey(r => r.Id);

        // Id sinh client-side (UUIDv7) — KHÔNG để DB tự sinh.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.FamilyId).IsRequired();

        // SHA-256 hex lower = 64 ký tự cố định.
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(64);

        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Property(r => r.RevokedReason).HasMaxLength(64);

        // Lookup theo hash khi refresh (hot-path).
        builder.HasIndex(r => r.TokenHash).HasDatabaseName("ix_refresh_hash");
        // Truy vấn theo user / thu hồi cả family.
        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_user");
        builder.HasIndex(r => r.FamilyId).HasDatabaseName("ix_refresh_family");
    }
}
