using Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF config <see cref="IdentityUser"/> (schema <c>identity</c>). Bảng <c>app_user</c> (KHÔNG "user" — từ khóa dành
/// riêng PostgreSQL). Unique <c>ux_identity_user_username</c> (username đã chuẩn hóa lower ở tầng use case/seeder).
/// Role lưu STRING (ổn định/đọc được — HasConversion&lt;string&gt;). Id UUIDv7 client-side → ValueGeneratedNever.
/// CreatedAt/UpdatedAt/actor do interceptor audit của PlatformDbContext quản.
/// </summary>
public sealed class IdentityUserConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("app_user");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Username).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200);

        builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("ux_identity_user_username");
    }
}
